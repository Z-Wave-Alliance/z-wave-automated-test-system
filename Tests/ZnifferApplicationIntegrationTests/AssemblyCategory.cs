// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using NUnit.Framework;

// These tests need real or emulated Z-Wave devices reachable over a serial API
// (or a Z/IP gateway). They are skipped in CI via this category.
[assembly: Category("DevicesNeeded")]
