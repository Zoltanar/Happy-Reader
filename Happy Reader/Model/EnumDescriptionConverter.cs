using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace Happy_Reader
{
    public class EnumDescriptionConverter : IValueConverter
    {
        private string GetEnumDescription(Enum enumObj)
        {
            if (enumObj == null) return "N/A";
            FieldInfo fieldInfo = enumObj.GetType().GetField(enumObj.ToString());
            object[] attribArray = fieldInfo.GetCustomAttributes(false);
            if (attribArray.Length == 0) return enumObj.ToString();
            DescriptionAttribute attrib = attribArray[0] as DescriptionAttribute;
            return attrib?.Description ?? enumObj.ToString();
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var myEnum = (Enum)value;
            string description = GetEnumDescription(myEnum);
            return description;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}
