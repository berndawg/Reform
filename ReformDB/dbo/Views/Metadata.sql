
CREATE VIEW Metadata
AS (
	SELECT 
		so.name AS [ObjectName], 
		sc.name AS [ColumnName], 
		st.name AS [ColumnType], 
		CAST(ISNULL(PK.IsPrimary,0) AS bit) AS [IsPrimary],
		CAST(COLUMNPROPERTY(sc.id, sc.name, 'IsIdentity') AS BIT) AS [IsIdentity],
		CAST(sc.isnullable AS BIT) AS IsNullable, 
		st.name AS [TypeName],
		CASE WHEN st.name IN ('varbinary','varchar','nvarchar','nchar','text','ntext') THEN 0 ELSE sc.length END AS [Length],
		CAST(CASE sc.xtype WHEN 165 THEN 1 ELSE 0 END AS BIT) AS [IsEncrypted],
		CAST(CASE WHEN st.name IN ('varbinary','varchar','nvarchar','nchar','text','ntext') AND sc.isnullable = 0 THEN 1 ELSE 0 END AS BIT) AS [IsRequired]
	FROM sysobjects so
	JOIN syscolumns sc ON so.id = sc.id
	JOIN systypes st ON sc.xtype=st.xtype

	LEFT JOIN (
		SELECT 1 AS [IsPrimary], C.TABLE_NAME, K.COLUMN_NAME
		FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS C
		JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS K ON C.TABLE_NAME = K.TABLE_NAME
														AND C.CONSTRAINT_CATALOG = K.CONSTRAINT_CATALOG
														AND C.CONSTRAINT_SCHEMA = K.CONSTRAINT_SCHEMA
														AND C.CONSTRAINT_NAME = K.CONSTRAINT_NAME
														AND C.CONSTRAINT_TYPE = 'PRIMARY KEY'
	) PK ON PK.TABLE_NAME = so.name AND PK.COLUMN_NAME = sc.name

	WHERE so.xtype in ('V','U') 
)