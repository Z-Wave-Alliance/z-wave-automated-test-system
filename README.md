# z-wave-automated-test-system

Z-Wave Automated Test System (aka ZATS) is a console application with
sets of services which allows to test capabilities and working state
of Z-Wave devices and its functionality in a network close to real
life. It also allows to perform negative testing (that shall not
happen in real life).

ZATS allows to create and run Test Cases (in C#).

ZATS is built on top of `z-wave-tools-core` (aka `Z-Wave DLL`).

Supported operating systems are Windows and Linux (with some
limitations) on several architectures (x86, ARM).

There are two versions of ZATS (and Tools Core), either based on .NET
Framework (default) or .NET (aka `netcore`).

## Usage

Running test cases is possible via command line or from Visual
Studio. In all cases they will be run by `ZatsTestConsole.exe`.

```
ZatsTestConsole --help
```

## References

- https://github.com/Z-Wave-Alliance/z-wave-automated-test-system
- https://github.com/Z-Wave-Alliance/z-wave-tools-core
- https://github.com/Z-Wave-Alliance/z-wave-blobs
- https://docs.silabs.com/z-wave/
- https://www.silabs.com/
- https://z-wavealliance.org/

## Legal info

SPDX-License-Identifier: BSD-3-Clause

SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
