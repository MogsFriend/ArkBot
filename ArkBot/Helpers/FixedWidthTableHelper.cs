﻿using ArkBot.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ArkBot.Helpers
{
    public static class FixedWidthTableHelper
    {
        public static string ToString<T>(IList<T> collection, Action<FixedWidthTableConfigurationBuilder<T>> configAction = null)
        {
            var builder = new FixedWidthTableConfigurationBuilder<T>();
            configAction?.Invoke(builder);
            var config = (FixedWidthTableConfigurationBuilder<T>.FixedWidthTableConfiguration)builder;

            var sb = new StringBuilder();
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(T));
            var array = new string[collection.Count + 1, props.Count];
            var columnsizes = new int[props.Count];
            var spacing = 2;
            var totals = new dynamic[props.Count];
            var totalsStr = new string[props.Count];
            for (int i = 0; i < props.Count; i++)
            {
                var pc = config.Get(props[i].Name);
                var value = pc?.Header ?? props[i].Name;
                array[0, i] = value;
                columnsizes[i] = value.Length > columnsizes[i] ? value.Length : columnsizes[i];
                if (pc != null && pc.Total && props[i].PropertyType.IsValueType) totals[i] = Activator.CreateInstance(props[i].PropertyType);
            }

            for (var i = 1; i <= collection.Count; i++)
            {
                var item = collection[i - 1];
                for (int j = 0; j < props.Count; j++)
                {
                    var pc = config.Get(props[j].Name);
                    var value = pc != null && pc.Format != null ? string.Format(pc.FormatProvider, "{0:" + pc.Format + "}", props[j].GetValue(item)) : props[j].GetValue(item).ToString();
                    array[i, j] = value;
                    columnsizes[j] = value.Length > columnsizes[j] ? value.Length : columnsizes[j];
                    if (pc != null && pc.Total) totals[j] += (dynamic)props[j].GetValue(item);

                    if (i == collection.Count && pc != null && pc.Total)
                    {
                        var total = pc != null && pc.Format != null ? string.Format(pc.FormatProvider, "{0:" + pc.Format + "}", totals[j]) : totals[j].ToString();
                        columnsizes[j] = total.Length > columnsizes[j] ? total.Length : columnsizes[j];
                        totalsStr[j] = total;
                    }
                }
            }

            for (var i = 0; i <= collection.Count; i++)
            {
                for (var j = 0; j < props.Count; j++)
                {
                    var pc = config.Get(props[j].Name);
                    var value = array[i, j];
                    if (pc != null)
                    {
                        if (pc.Alignment < 0) value = value.PadRight(columnsizes[j] + spacing);
                        else if (pc.Alignment > 0) value = value.PadLeft(columnsizes[j] + spacing);
                        else value = value.PadBoth(columnsizes[j] + spacing);
                    }
                    else value = value.PadRight(columnsizes[j] + spacing);
                    sb.Append(value);
                }
                sb.AppendLine();
            }

            if (totals.Any(x => x != null))
            {
                sb.AppendLine(new string('-', columnsizes.Sum() + props.Count * spacing));
                for (int i = 0; i < props.Count; i++)
                {
                    if (totals[i] == null)
                    {
                        sb.Append(new string(' ', columnsizes[i] + spacing));
                        continue;
                    }

                    var pc = config.Get(props[i].Name);
                    var value = totalsStr[i];
                    if (pc != null)
                    {
                        if (pc.Alignment < 0) value = value.PadRight(columnsizes[i] + spacing);
                        else if (pc.Alignment > 0) value = value.PadLeft(columnsizes[i] + spacing);
                        else value = value.PadBoth(columnsizes[i] + spacing);
                    }
                    else value = value.PadRight(columnsizes[i] + spacing);
                    sb.Append(value);
                }
            }

            return sb.ToString().TrimEnd('\r', '\n');
        }
    }

    public class FixedWidthTableConfigurationBuilder<T>
    {
        private FixedWidthTableConfiguration _configuration;

        public FixedWidthTableConfigurationBuilder()
        {
            _configuration = new FixedWidthTableConfiguration();
        }

        /// <param name="alignment">-1: left, 0: middle, 1: right</param>
        public FixedWidthTableConfigurationBuilder<T> For(Expression<Func<T, object>> selector, string header = null, int alignment = -1, string format = null, IFormatProvider formatProvider = null, bool total = false)
        {
            var name = ((selector.Body as UnaryExpression)?.Operand as MemberExpression)?.Member?.Name;
            if (name != null) _configuration.Add(name, header, alignment, format, formatProvider, total);

            return this;
        }

        static public explicit operator FixedWidthTableConfiguration(FixedWidthTableConfigurationBuilder<T> self)
        {
            return self._configuration;
        }

        public class FixedWidthTableConfiguration
        {
            public Dictionary<string, FixedWidthTableConfigurationProperty> Properties { get; set; }

            public FixedWidthTableConfiguration()
            {
                Properties = new Dictionary<string, FixedWidthTableConfigurationProperty>();
            }

            public void Add(string name, string header = null, int alignment = -1, string format = null, IFormatProvider formatProvider = null, bool total = false)
            {
                Properties.Add(name, new FixedWidthTableConfigurationProperty { Header = header, Alignment = alignment, Format = format, FormatProvider = formatProvider, Total = total });
            }

            public FixedWidthTableConfigurationProperty Get(string name)
            {
                return Properties.ContainsKey(name) ? Properties[name] : null;
            }
        }

        public class FixedWidthTableConfigurationProperty
        {
            public string Header { get; set; }
            public string Format { get; set; }
            public IFormatProvider FormatProvider { get; set; }
            public int Alignment { get; set; }
            public bool Total { get; set; }
        }
    }
}
