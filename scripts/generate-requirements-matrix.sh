#!/usr/bin/env bash
set -euo pipefail

# Script to generate requirements-test-coverage.md from test method annotations
# This script scans all test files for [Trait("Requirement", "FR-X.X")] and [Trait("Requirement", "TR-X.X")]
# annotations and generates a traceability matrix

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
TESTS_DIR="$REPO_ROOT/tests"
OUTPUT_FILE="$REPO_ROOT/docs/requirements-test-coverage.md"
FR_FILE="$REPO_ROOT/docs/functional-requirements.md"
TR_FILE="$REPO_ROOT/docs/technical-requirements.md"

echo "Scanning test files for requirement annotations..."

# Temporary file for storing requirement mappings
TEMP_FR_MAP=$(mktemp)
TEMP_TR_MAP=$(mktemp)
trap 'rm -f "$TEMP_FR_MAP" "$TEMP_TR_MAP"' EXIT

# Extract all FR and TR IDs from requirements files
extract_requirement_ids() {
    local req_file="$1"
    local prefix="$2"
    # Match patterns like "- **FR-1.1**" or "- **TR-1.1**"
    grep -oP "(?<=\*\*${prefix}-)[0-9]+(\.[0-9]+)*(?=\*\*)" "$req_file" 2>/dev/null || true
}

echo "Extracting requirement IDs from requirement documents..."
FR_IDS=$(extract_requirement_ids "$FR_FILE" "FR" | sort -V | uniq)
TR_IDS=$(extract_requirement_ids "$TR_FILE" "TR" | sort -V | uniq)

# Function to scan test files and extract requirement mappings
scan_test_files() {
    local req_prefix="$1"
    local output_map="$2"
    
    # Find all test files
    find "$TESTS_DIR" -name "*Tests.cs" -type f | while read -r test_file; do
        local rel_path="${test_file#$REPO_ROOT/}"
        local class_name=""
        local class_requirements=()
        
        # Read file with awk to better handle state
        awk -v prefix="$req_prefix" -v outfile="$output_map" -v filepath="$rel_path" '
            BEGIN {
                in_class = 0
                is_test_method = 0
            }
            
            # Match class-level Trait annotations (before class declaration)
            !in_class && /\[Trait\("Requirement", "'"$req_prefix"'-[0-9.]+"\)/ {
                match($0, /"'"$req_prefix"'-([0-9.]+)"/, arr)
                class_reqs[++class_req_count] = prefix "-" arr[1]
            }
            
            # Match class declaration
            /public class [a-zA-Z0-9_]+/ {
                match($0, /public class ([a-zA-Z0-9_]+)/, arr)
                class_name = arr[1]
                in_class = 1
            }
            
            # Match method-level Trait annotations (inside class)
            in_class && /\[Trait\("Requirement", "'"$req_prefix"'-[0-9.]+"\)/ {
                match($0, /"'"$req_prefix"'-([0-9.]+)"/, arr)
                method_reqs[++method_req_count] = prefix "-" arr[1]
            }
            
            # Match test attributes
            in_class && /\[(Fact|Theory)\]/ {
                is_test_method = 1
            }
            
            # Match method declaration
            in_class && /public (async )?(Task|void) [a-zA-Z0-9_]+\(/ {
                match($0, /public (async )?(Task|void) ([a-zA-Z0-9_]+)\(/, arr)
                method_name = arr[3]
                
                if (is_test_method) {
                    # Use method-level requirements if available, otherwise class-level
                    if (method_req_count > 0) {
                        for (i = 1; i <= method_req_count; i++) {
                            print method_reqs[i] "|" class_name "." method_name "|" filepath "|method" >> outfile
                        }
                    } else if (class_req_count > 0) {
                        for (i = 1; i <= class_req_count; i++) {
                            print class_reqs[i] "|" class_name "." method_name "|" filepath "|class" >> outfile
                        }
                    }
                }
                
                # Reset method-level state
                method_req_count = 0
                delete method_reqs
                is_test_method = 0
            }
        ' "$test_file"
    done
}

echo "Scanning for FR requirements..."
scan_test_files "FR" "$TEMP_FR_MAP"

echo "Scanning for TR requirements..."
scan_test_files "TR" "$TEMP_TR_MAP"

# Function to generate coverage row for a requirement
generate_coverage_row() {
    local req_id="$1"
    local map_file="$2"
    
    # Find all tests covering this requirement
    local tests=$(grep "^${req_id}|" "$map_file" 2>/dev/null | sort -u || true)
    
    if [[ -z "$tests" ]]; then
        echo "| $req_id | None | None |"
    else
        # Determine coverage level
        local coverage="Covered"
        
        # Group by class to determine if we should use ClassName.* notation
        declare -A class_methods
        declare -A class_files
        declare -A method_specific
        
        while IFS='|' read -r req method file source_type; do
            [[ -z "$method" ]] && continue
            local class="${method%%.*}"
            local method_name="${method#*.}"
            
            if [[ "$source_type" == "method" ]]; then
                # Method has specific requirements
                method_specific["$class|$method"]="$file"
            else
                # Method inherits class requirements
                if [[ -z "${class_methods[$class]:-}" ]]; then
                    class_methods["$class"]="$method_name"
                else
                    class_methods["$class"]="${class_methods[$class]} $method_name"
                fi
                class_files["$class"]="$file"
            fi
        done <<< "$tests"
        
        # Format tests list
        local tests_formatted=""
        
        # First, output classes with all methods (using .*)
        for class in "${!class_methods[@]}"; do
            if [[ -n "$tests_formatted" ]]; then
                tests_formatted="${tests_formatted}, "
            fi
            tests_formatted="${tests_formatted}\`${class}.*\` (\`${class_files[$class]}\`)"
        done
        
        # Then, output individual methods with specific requirements
        for key in "${!method_specific[@]}"; do
            local method="${key#*|}"
            local file="${method_specific[$key]}"
            if [[ -n "$tests_formatted" ]]; then
                tests_formatted="${tests_formatted}, "
            fi
            tests_formatted="${tests_formatted}\`${method}\` (\`${file}\`)"
        done
        
        echo "| $req_id | $coverage | $tests_formatted |"
    fi
}

echo "Generating requirements-test-coverage.md..."

# Generate the output file
{
    echo "# Requirements Test Coverage"
    echo ""
    echo "This document maps every requirement ID in \`docs/functional-requirements.md\` (FR) and \`docs/technical-requirements.md\` (TR) to current automated test coverage."
    echo ""
    echo "**Note:** This file is auto-generated by \`scripts/generate-requirements-matrix.sh\`. Do not edit manually."
    echo ""
    echo "Coverage status values:"
    echo "- \`Covered\`: direct automated test coverage exists."
    echo "- \`Partial\`: some behavior is covered, but not the full requirement scope."
    echo "- \`None\`: no automated test currently covers the requirement."
    echo ""
    echo "## Functional Requirements (FR)"
    echo ""
    echo "| Requirement | Coverage | Tests |"
    echo "|---|---|---|"
    
    # Generate FR rows
    while IFS= read -r fr_id; do
        if [[ -n "$fr_id" ]]; then
            generate_coverage_row "FR-${fr_id}" "$TEMP_FR_MAP"
        fi
    done <<< "$FR_IDS"
    
    echo ""
    echo "## Technical Requirements (TR)"
    echo ""
    echo "| Requirement | Coverage | Tests |"
    echo "|---|---|---|"
    
    # Generate TR rows
    while IFS= read -r tr_id; do
        if [[ -n "$tr_id" ]]; then
            generate_coverage_row "TR-${tr_id}" "$TEMP_TR_MAP"
        fi
    done <<< "$TR_IDS"
    
} > "$OUTPUT_FILE"

echo "✅ Generated $OUTPUT_FILE"
echo "Total FR IDs: $(echo "$FR_IDS" | grep -c . || echo 0)"
echo "Total TR IDs: $(echo "$TR_IDS" | grep -c . || echo 0)"
echo "FR mappings: $(wc -l < "$TEMP_FR_MAP")"
echo "TR mappings: $(wc -l < "$TEMP_TR_MAP")"
