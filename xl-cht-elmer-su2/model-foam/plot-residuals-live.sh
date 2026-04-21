#!/usr/bin/env bash
set -euo pipefail

# Live residual plotter for OpenFOAM logs.
# Usage:
#   ./plot-residuals-live.sh [log-file] [refresh-seconds]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "${SCRIPT_DIR}"

LOG_FILE="${1:-}"
REFRESH_SECS="${2:-10}"

if [[ -z "${LOG_FILE}" ]]; then
    if [[ -f "log.8.foamMultiRun" ]]; then
        LOG_FILE="log.8.foamMultiRun"
    elif [[ -f "log.8.foamMultirun" ]]; then
        LOG_FILE="log.8.foamMultirun"
    else
        echo "Error: no log file provided and neither log.8.foamMultiRun nor log.8.foamMultirun exists." >&2
        exit 1
    fi
fi

if [[ ! -f "${LOG_FILE}" ]]; then
    echo "Error: log file not found: ${LOG_FILE}" >&2
    exit 1
fi

if ! command -v gnuplot >/dev/null 2>&1; then
    echo "Error: gnuplot is not installed or not in PATH." >&2
    exit 1
fi

if ! [[ "${REFRESH_SECS}" =~ ^[0-9]+([.][0-9]+)?$ ]]; then
    echo "Error: refresh seconds must be a positive number." >&2
    exit 1
fi

TMP_DIR=".residual_plot_tmp"
mkdir -p "${TMP_DIR}"

EXTRACT_SCRIPT="${TMP_DIR}/extract-residuals.sh"
cat > "${EXTRACT_SCRIPT}" <<'EOF_EXTRACT'
#!/usr/bin/env bash
set -euo pipefail

src_log="$1"
out_dir="$2"

rm -f "${out_dir}"/*.dat

awk -v out_dir="${out_dir}" '
    {
        if (match($0, /Solving for [^,]+, Initial residual = [0-9.eE+-]+/)) {
            line = substr($0, RSTART, RLENGTH)
            sub(/^Solving for /, "", line)
            split(line, parts, ", Initial residual = ")
            field = parts[1]
            residual = parts[2]

            # Sanitize field name for file naming and gnuplot legends.
            gsub(/[^A-Za-z0-9_]/, "_", field)

            count[field] += 1
            file = out_dir "/residual_" field ".dat"
            print count[field], residual >> file
            close(file)
        }
    }
' "${src_log}"
EOF_EXTRACT
chmod +x "${EXTRACT_SCRIPT}"

# Initial extraction so the first plot has data if available.
"${EXTRACT_SCRIPT}" "${LOG_FILE}" "${TMP_DIR}"

gnuplot -persist <<EOF
set grid
set key outside right top
set xlabel "Iteration index (per field)"
set ylabel "Initial residual"
set title sprintf("OpenFOAM residuals: %s", "${LOG_FILE}")
set logscale y

refresh = ${REFRESH_SECS}
logfile = "${LOG_FILE}"
outdir = "${TMP_DIR}"
extractor = "${EXTRACT_SCRIPT}"

extractCmd = sprintf("bash '%s' '%s' '%s'", extractor, logfile, outdir)
listCmd = sprintf("ls -1 '%s'/residual_*.dat 2>/dev/null", outdir)

system(extractCmd)
files = system(listCmd)

if (strlen(files) > 0) {
    plot for [f in files] f using 1:2 with lines lw 2 title f
} else {
    plot NaN title "No residual data yet"
}

pause refresh
reread
EOF
