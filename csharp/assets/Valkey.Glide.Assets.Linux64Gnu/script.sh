

                    TARGET="x86_64-unknown-linux-gnu"
                    echo "Searching for TARGET=$TARGET"

                    found=0

                    find . -type f -name "*.csproj" -print0 | while IFS= read -r -d '' csproj_file; do
                        rust_tuple=$(xmllint --xpath 'string(//Project/PropertyGroup/RustTuple)' "$csproj_file" 2>/dev/null || true)

                        if [[ -z "$rust_tuple" ]]; then
                            echo "Skipping: $csproj_file"
                            continue
                        fi

                        echo "Checking $csproj_file : RustTuple=$rust_tuple"

                        if [[ "$rust_tuple" == "$TARGET" ]]; then
                            echo "Match found: $csproj_file"

                            echo "DOTNET_PROJECT_PATH=$csproj_file" >> "$GITHUB_ENV"
                            found=1
                            break
                        fi
                    done

                    if [[ "$found" -eq 0 ]]; then
                        echo "No matching project found for TARGET=$TARGET"
                        exit 1
                    fi


