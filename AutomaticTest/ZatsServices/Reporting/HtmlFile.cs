// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.IO;

namespace Zats.Services
{
    public class HtmlFile
    {
        private string _fileName;
        public HtmlFile(string fileName)
        {
            _fileName = fileName;
        }

        public HtmlTable Table(params string[] headers)
        {
            return new HtmlTable(this, 600, 500, headers);
        }

        public HtmlTable StackedColumnChart(int width, int height, params string[] headers)
        {
            return new StackedColumnConfluenceChart(this, width, height, headers);
        }

        public HtmlTable StackedColumnChart(params string[] headers)
        {
            return new StackedColumnConfluenceChart(this, 600, 500, headers);
        }

        public HtmlTable PieChart(int width, int height, params string[] headers)
        {
            return new PieConfluenceChart(this, width, height, headers);
        }

        public HtmlTable PieChart(params string[] headers)
        {
            return new PieConfluenceChart(this, 600, 500, headers);
        }

        public HtmlTable LineChart(int width, int height, params string[] headers)
        {
            return new LineConfluenceChart(this, width, height, headers);
        }

        public HtmlTable LineChart(params string[] headers)
        {
            return new LineConfluenceChart(this, 600, 500, headers);
        }

        public void Paragraph(string text)
        {
            AppendLine($"<p>{text}</p>");
        }

        public void Paragraph(TextFormat format, string text)
        {
            switch (format)
            {
                case TextFormat.Strong:
                    AppendLine($"<p><strong>{text}</strong></p>");
                    break;
                case TextFormat.Underline:
                    AppendLine($"<p><u>{text}</u></p>");
                    break;
                case TextFormat.Code:
                    AppendLine($"<p><code>{text}</code></p>");
                    break;
                case TextFormat.Strikethrough:
                    AppendLine($"<p><s>{text}</s></p>");
                    break;
                default:
                    AppendLine($"<p>{text}</p>");
                    break;
            }
        }

        public void HeadingL1(string text)
        {
            AppendLine($"<h1>{text}</h1>");
        }

        public void HeadingL2(string text)
        {
            AppendLine($"<h2>{text}</h2>");
        }

        public void HeadingL3(string text)
        {
            AppendLine($"<h3>{text}</h3>");
        }

        public void Preformated(string text)
        {
            AppendLine($"<pre>{text}</pre>");
        }

        private void AppendLine(string text)
        {
            File.AppendAllText(_fileName, $"{text}{Environment.NewLine}");
        }

        public class HtmlTableCell : IDisposable
        {
            protected HtmlFile _parent;
            public HtmlTableCell(HtmlFile parent)
            {
                _parent = parent;
                OpenCell();
            }

            protected virtual void OpenCell()
            {
                _parent.AppendLine("    <td>");
            }

            protected virtual HtmlTableCell CloseCell()
            {
                _parent.AppendLine("    </td>");
                return this;
            }

            public void Dispose()
            {
                CloseCell();
            }
        }

        public class HtmlTableRow : IDisposable
        {
            protected HtmlFile _parent;
            public HtmlTableRow(HtmlFile parent)
            {
                _parent = parent;
                OpenRow();
            }

            public HtmlTableCell Cell()
            {
                return new HtmlTableCell(_parent);
            }

            protected virtual void OpenRow()
            {
                _parent.AppendLine("    <tr>");
            }

            protected virtual HtmlTableRow CloseRow()
            {
                _parent.AppendLine("    </tr>");
                return this;
            }

            public void Dispose()
            {
                CloseRow();
            }
        }

        public class HtmlTable : IDisposable
        {
            protected HtmlFile _parent;
            protected string[] _headers;
            protected int _width;
            protected int _height;

            public HtmlTable(HtmlFile parent, int width, int height, params string[] headers)
            {
                _parent = parent;
                _headers = headers;
                _width = width;
                _width = Math.Min(_width, 2000);
                _width = Math.Max(_width, 100);
                _height = height;
                _height = Math.Min(_height, 2000);
                _height = Math.Max(_height, 100);
                OpenTable();
            }

            protected virtual void OpenTable()
            {
                _parent.AppendLine("<table>");
                _parent.AppendLine("  <tbody>");
                _parent.AppendLine("    <tr>");
                foreach (var item in _headers)
                {
                    _parent.AppendLine("      <th>");
                    _parent.AppendLine($"        {item}");
                    _parent.AppendLine("      </th>");
                }
                _parent.AppendLine("    </tr>");
            }

            public HtmlTable AddRow(params string[] cells)
            {
                _parent.AppendLine("    <tr>");
                foreach (var item in cells)
                {
                    _parent.AppendLine("      <td>");
                    _parent.AppendLine($"        {item}");
                    _parent.AppendLine("      </td>");
                }
                _parent.AppendLine("    </tr>");
                return this;
            }

            protected virtual HtmlTable CloseTable()
            {
                _parent.AppendLine("  </tbody>");
                _parent.AppendLine("</table>");
                return this;
            }

            public void Dispose()
            {
                CloseTable();
            }

            public HtmlTableRow Row()
            {
                return new HtmlTableRow(_parent);
            }
        }

        public abstract class ConfluenceChart : HtmlTable
        {
            protected static string Color = "#2484c1"; //#d04437
            public ConfluenceChart(HtmlFile parent, int width, int height, params string[] headers)
                : base(parent, width, height, headers)
            {

            }

            protected virtual string GetColor()
            {
                return Color;
            }

            protected abstract string GetChartType();

            protected override void OpenTable()
            {
                if (_headers.Length == 2)
                {
                    var chartType = GetChartType();
                    _parent.AppendLine("<ac:structured-macro ac:name=\"table-chart\" ac:schema-version=\"1\">");
                    _parent.AppendLine($"  <ac:parameter ac:name=\"tfc-width\">{_width}</ac:parameter>");
                    _parent.AppendLine($"  <ac:parameter ac:name=\"tfc-height\">{_height}</ac:parameter>");
                    _parent.AppendLine($"  <ac:parameter ac:name=\"column\">{_headers[0]}</ac:parameter>");
                    _parent.AppendLine($"  <ac:parameter ac:name=\"aggregation\">{_headers[1]}</ac:parameter>");
                    if (chartType != null)
                    {
                        _parent.AppendLine($"  <ac:parameter ac:name=\"type\">{chartType}</ac:parameter>");
                        _parent.AppendLine($"  <ac:parameter ac:name=\"colors\">{GetColor()}</ac:parameter>");
                    }
                    _parent.AppendLine("  <ac:parameter ac:name=\"hide\">true</ac:parameter>");
                    _parent.AppendLine("  <ac:rich-text-body>");
                }
                _parent.AppendLine("    <table>");
                _parent.AppendLine("      <tbody>");
                _parent.AppendLine("        <tr>");
                foreach (var item in _headers)
                {
                    _parent.AppendLine("          <th>");
                    _parent.AppendLine($"            {item}");
                    _parent.AppendLine("          </th>");
                }
                _parent.AppendLine("        </tr>");
            }

            protected override HtmlTable CloseTable()
            {
                _parent.AppendLine("      </tbody>");
                _parent.AppendLine("    </table>");
                if (_headers.Length == 2)
                {
                    _parent.AppendLine("  </ac:rich-text-body>");
                    _parent.AppendLine("</ac:structured-macro>");
                }
                return this;
            }
        }

        public class StackedColumnConfluenceChart : ConfluenceChart
        {
            public static string ChartType = "Stacked Column";
            public StackedColumnConfluenceChart(HtmlFile parent, int width, int height, params string[] headers)
                : base(parent, width, height, headers)
            {
            }

            protected override string GetChartType()
            {
                return ChartType;
            }
        }

        public class PieConfluenceChart : ConfluenceChart
        {
            public PieConfluenceChart(HtmlFile parent, int width, int height, params string[] headers)
                : base(parent, width, height, headers)
            {
            }

            protected override string GetChartType()
            {
                return null;
            }
        }

        public class LineConfluenceChart : ConfluenceChart
        {
            public static string ChartType = "Line";

            public LineConfluenceChart(HtmlFile parent, int width, int height, params string[] headers)
                : base(parent, width, height, headers)
            {
            }

            protected override string GetChartType()
            {
                return ChartType;
            }
        }

        public enum TextFormat
        {
            Normal,
            Strong, // strong
            Underline, // u
            Code, // code
            Strikethrough, // s
        }
    }
}
