using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Windows.Markup;

namespace RadiusYandex.Models
{
    public enum TaskType
    {
        [Description("Загрузка на Яндекс.Диск")]
        UPLOAD,
        [Description("Загрузка с Яндекс.Диска")]
        DOWNLOAD
    }


    public class EnumerationExtension : MarkupExtension
    {
        private Type _enumType;


        public EnumerationExtension(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");

            EnumType = enumType;
        }

        public Type EnumType
        {
            get { return _enumType; }
            private set
            {
                if (_enumType == value)
                    return;

                var enumType = Nullable.GetUnderlyingType(value) ?? value;

                if (enumType.IsEnum == false)
                    throw new ArgumentException("Type must be an Enum.");

                _enumType = value;
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var enumValues = Enum.GetValues(EnumType);

            return (
              from object enumValue in enumValues
              select new EnumerationMember
              {
                  Value = enumValue,
                  Description = GetDescription(enumValue)
              }).ToArray();
        }

        private string GetDescription(object enumValue)
        {
            var descriptionAttribute = EnumType
              .GetField(enumValue.ToString())
              .GetCustomAttributes(typeof(DescriptionAttribute), false)
              .FirstOrDefault() as DescriptionAttribute;


            return descriptionAttribute != null
              ? descriptionAttribute.Description
              : enumValue.ToString();
        }

        public class EnumerationMember
        {
            public string Description { get; set; }
            public object Value { get; set; }
        }
    }

    [Serializable]
    public class Job
    {
        public Job()
        {
        }

        public Job (Guid id, string localpath, string externalpath)
        {
            ID = id;
            LocalPath = localpath;
            ExternalPath = externalpath;
        }

        public TaskType Type { get; set; }
        public bool Active { get; set; }
        public Guid ID { get; set; }
        public string LocalPath { get; set; }
        public string ExternalPath { get; set; }
        public string Filter { get; set; }
        public bool DeleteFiles { get; set; }
        [XmlIgnore]
        public string Status { get; set; }
        [XmlIgnore]
        public int Percent { get; set; }
    }
}
