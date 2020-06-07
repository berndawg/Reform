﻿CREATE TABLE [dbo].[Airport](
	[AirportID] [int] IDENTITY(1,1) NOT NULL,
	[AirportCode] [varchar](10) NOT NULL,
	[AirportName] [varchar](50) NOT NULL,
	[CountryId] [int] NOT NULL,
 CONSTRAINT [PK_Airport] PRIMARY KEY CLUSTERED 
(
	[AirportID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
