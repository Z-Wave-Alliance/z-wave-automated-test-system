#!/usr/bin/make -f
# -*- makefile -*-
# ex: set tabstop=4 noexpandtab:
# -*- coding: utf-8 -*
#
# SPDX-License-Identifier: BSD-3-Clause
# SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
# SPDX-License-URL: https://spdx.org/licenses/BSD-3-Clause

project?=z-wave-automated-test-system

default: help prepare all test check

# TODO: https://github.com/Z-Wave-Alliance/z-wave-automated-test-system/issues/22
z_wave_blobs_url?=ssh://git@github.com/Z-Wave-Alliance/z-wave-blobs
z_wave_blobs_ref?=v0.1.0

z_wave_tools_core_url?=ssh://git@github.com/Z-Wave-Alliance/z-wave-tools-core
z_wave_tools_core_ref?=v25.1.1
# z_wave_tools_core_dir?=z-wave-tools-core
# TODO: https://github.com/Z-Wave-Alliance/z-wave-automated-test-system/issues/21"
z_wave_tools_core_dir?=${CURDIR}/../z-wave-tools-core


SELF?=Makefile

os_name?=debian
# TODO: keep aligned to stable
os_version?=12

dotnet_version?=6.0
dotnet_configuration?=Release
dotnet_framework?=net${dotnet_version}
dotnet_filename?=packages-microsoft-prod.deb
dotnet_url?=https://packages.microsoft.com/config/${os_name}/${os_version}/${dotnet_filename}
dotnet_debian_package?=dotnet-sdk-${dotnet_version}
dotnet?=DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet
dotnet_runtime=linux-x64
dotnet_framework?=net${dotnet_version}

sln?=ZatsBuild_netcore.sln
app?=AutomaticTest/ZatsTestConsole/bin/${dotnet_configuration}/net48/ZatsTestConsole.exe
wine?=wine

debian_packages+=make sudo wget
debian_packages+=git

default: help all

help:
	@echo "# PATH=${PATH}"
	egrep -o '^[^:]*:' ${SELF}

help/all: README.md
	@echo "# ${project}: ${url}"
	@echo ""
	@head $^
	@echo ""
	@echo "# Available ${SELF}rules at your own risk:"
	@grep -o '^[^ ]*:' ${SELF} \
		| grep -v '\$$' | grep -v '^#' | grep -v '^\.' \
		| grep -v '=' | grep -v '%'
	@echo "#"
	@echo "# Environment:"
	@echo "#  PATH=${PATH}"
	@echo ""
	@echo ""

setup: debian/setup dotnet/setup wine/setup

test: run

check: test

all: sln/prepare sln/all

run: sln/run

prepare: git/prepare

git/prepare: git/modules/prepare

${z_wave_tools_core_dir}:
	@mkdir -p ${@D}
	git clone ${z_wave_tools_core_url} --branch=${z_wave_tools_core_ref} --recursive $@

${z_wave_tools_core_dir}/lib: z-wave-blobs/z-wave-tools-core
	ln -fs ${CURDIR}/$< $@

z-wave-blobs:
	@echo "TODO: https://github.com/Z-Wave-Alliance/z-wave-blobs/issues/5"
	git clone ${z_wave_blobs_url} --branch=${z_wave_blobs_ref} --recursive $@

git/modules/prepare: ${z_wave_tools_core_dir} z-wave-blobs ${z_wave_tools_core_dir}/lib
	@echo "TODO: https://github.com/Z-Wave-Alliance/OSWG/discussions/55"

sln/prepare: ${z_wave_tools_core_dir}/README.md
	@echo "TODO: https://github.com/Z-Wave-Alliance/z-wave-automated-test-system/issues/21"
	[ "${<D}" != "z-wave-tools-core" ] || { rm -f ../${<D} && mv ${CURDIR}/${<D} .. ; }

/etc/debian_version:
	@echo "Please port to other OS than ${@F}"
	ls $@

debian/setup: /etc/debian_version
	sudo apt update
	sudo apt install -y ${debian_packages}

dotnet/setup:
	sudo wget ${dotnet_url}
	sudo apt update
	sudo apt install ./${dotnet_filename}
	rm ${dotnet_filename}
	sudo apt update
	sudo apt-get install -y ${dotnet_debian_package}
	@echo "TODO: https://github.com/dotnet/sdk/issues/24474"
	${dotnet} --version

wine/setup:
	${sudo} dpkg --add-architecture i386
	${sudo} apt-get update
	${sudo} apt-get install -y wine32:i386

sln/all: ${app}

${app}: ${sln}
	${dotnet} build ${<} --configuration ${dotnet_configuration}
	ls -l ${app}

sln/run: ${app}
	${wine} ${app} --help
