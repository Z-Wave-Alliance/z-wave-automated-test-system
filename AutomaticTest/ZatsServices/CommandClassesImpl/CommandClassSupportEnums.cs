// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services.CommandClassesImpl
{
    public enum UiControl
    {
        Switch,
        TextBox,
        CheckBox,
        Button,
        ComboBox,
        RadioButton,
        ListComboBox,
        ListCheckBox,
    }

    public enum InputType
    {
        none,
        Boolean,
        Text,           // ASCII
        Text_127,     // ASCII 0...127
        TextUTF8,
        Hex,
        Hex8,
        Hex16,
        Hex24,
        Hex32,
        Hex128,
        HexByteArray,
        Integer,
        Int_0_7,
        Int_0_9,
        Int_0_24,
        Int_0_59,
        Int_0_254,
        Int_0_255,
        Int_0_100_and_255,
        Int_0_4095,
        Int_1_127,
        Int_1_232,
        Int_1_254,
        Int_1_255,
        Int_2_255,
        Enum,
        UShort,
        Double,
        DurationSec
    }
}
