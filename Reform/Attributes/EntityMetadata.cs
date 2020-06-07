// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.

using System;

namespace Reform.Attributes
{
    public class EntityMetadata : Attribute
    {
        public string DatabaseName { get; set; }
        public string SymmetricKeyName { get; set; }
        public string SymmetricKeyCertificate { get; set; }
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public string TableAlias { get; set; }
        public string Joins { get; set; }
    }
}