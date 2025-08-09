#!/bin/sh

set -e
if ! command -v mono >/dev/null 2>&1; then
	command -v dotnet >/dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK requires dotnet or mono."; exit 1; }
fi

if command -v python3 >/dev/null 2>&1; then
	PYTHON="python3"
else
	command -v python >/dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK requires python."; exit 1; }
	PYTHON="python"
fi

require_variables() {
	missing=""
	for i in "$@"; do
		eval check="\$$i"
		[ -z "${check}" ] && missing="${missing}   ${i}\n"
	done
	if [ ! -z "${missing}" ]; then
		echo "Required mod.config variables are missing:\n${missing}Repair your mod.config (or user.config) and try again."
		exit 1
	fi
}

TEMPLATE_LAUNCHER=$(${PYTHON} -c "import os; print(os.path.realpath('$0'))")
TEMPLATE_ROOT=$(dirname "${TEMPLATE_LAUNCHER}")
MOD_SEARCH_PATHS="${TEMPLATE_ROOT}/mods,./mods"

# shellcheck source=mod.config
. "${TEMPLATE_ROOT}/mod.config"

if [ -f "${TEMPLATE_ROOT}/user.config" ]; then
	# shellcheck source=user.config
	. "${TEMPLATE_ROOT}/user.config"
fi
#!/bin/sh
set -o errexit || exit $?

ENGINEDIR=$(dirname "$0")

if command -v python3 >/dev/null 2>&1; then
	 LAUNCHPATH=$(python3 -c 'import os,sys; print(os.path.realpath(sys.argv[1]))' "$0")
else
	 LAUNCHPATH=$(python -c 'import os,sys; print(os.path.realpath(sys.argv[1]))' "$0")
fi

# Prompt for a mod to launch if one is not already specified
MODARG=''
if [ z"${*#*Game.Mod=}" = z"$*" ]
then
	if command -v zenity > /dev/null
	then
		TITLE=$(zenity --title='Launch OpenRA' --list --hide-header --text 'Select game mod:' --column 'Game mod' 'Red Alert' 'Tiberian Dawn' 'Dune 2000' 'Tiberian Sun' || echo "cancel")
		if [ "$TITLE" = "Tiberian Dawn" ]; then MODARG='Game.Mod=cnc'
		elif [ "$TITLE" = "Dune 2000" ]; then MODARG='Game.Mod=d2k'
		elif [ "$TITLE" = "Tiberian Sun" ]; then MODARG='Game.Mod=ts'
		elif [ "$TITLE" = "Red Alert" ]; then MODARG='Game.Mod=ra'
		else exit 0
		fi
	else
		echo "Please provide the Game.Mod=\$MOD argument (possible \$MOD values: ra, cnc, d2k, ts)"
		exit 1
	fi
fi

# Launch the engine with the appropriate arguments
dotnet "${ENGINEDIR}/bin/OpenRA.dll" Engine.EngineDir=".." Engine.LaunchPath="${LAUNCHPATH}" ${MODARG} "$@" && rc=0 || rc=$?

# Show a crash dialog if something went wrong
if [ "${rc}" != 0 ] && [ "${rc}" != 1 ]; then
	if [ "$(uname -s)" = "Darwin" ]; then
		LOGS="${HOME}/Library/Application Support/OpenRA/Logs/"
	else
		LOGS="${XDG_CONFIG_HOME:-${HOME}/.config}/openra/Logs"
		if [ ! -d "${LOGS}" ] && [ -d "${HOME}/.openra/Logs" ]; then
			LOGS="${HOME}/.openra/Logs"
		fi
	fi

	if [ -d Support/Logs ]; then
		LOGS="${PWD}/Support/Logs"
	fi
	ERROR_MESSAGE=$(printf "%s has encountered a fatal error.\nPlease refer to the crash logs and FAQ for more information.\n\nLog files are located in %s\nThe FAQ is available at https://wiki.openra.net/FAQ" "OpenRA" "${LOGS}")
	if command -v zenity > /dev/null; then
		zenity --no-wrap --error --title "OpenRA" --no-markup --text "${ERROR_MESSAGE}" 2> /dev/null || :
	elif command -v kdialog > /dev/null; then
		kdialog --title "OpenRA" --error "${ERROR_MESSAGE}" || :
	else
		echo "${ERROR_MESSAGE}"
	fi
	exit 1
fi
require_variables "MOD_ID" "ENGINE_VERSION" "ENGINE_DIRECTORY"

cd "${TEMPLATE_ROOT}"
if [ ! -f "${ENGINE_DIRECTORY}/bin/OpenRA.dll" ] || [ "$(cat "${ENGINE_DIRECTORY}/VERSION")" != "${ENGINE_VERSION}" ]; then
	echo "Required engine files not found."
	echo "Run \`make\` in the mod directory to fetch and build the required files, then try again.";
	exit 1
fi

if command -v mono >/dev/null 2>&1 && [ "$(grep -c .NETCoreApp,Version= ${ENGINE_DIRECTORY}/bin/OpenRA.dll)" = "0" ]; then
	RUNTIME_LAUNCHER="mono --debug"
else
	RUNTIME_LAUNCHER="dotnet"
fi

cd "${ENGINE_DIRECTORY}"
${RUNTIME_LAUNCHER} bin/OpenRA.dll Game.Mod="${MOD_ID}" Engine.EngineDir=".." Engine.LaunchPath="${TEMPLATE_LAUNCHER}" Engine.ModSearchPaths="${MOD_SEARCH_PATHS}" "$@"
