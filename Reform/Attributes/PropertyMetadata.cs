// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.

using System;
using Reform.Enum;

namespace Reform.Attributes
{
    public class PropertyMetadata : Attribute
    {
        public string ColumnName { get; set; }
        public string DisplayName { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsRequired { get; set; }
    }
}