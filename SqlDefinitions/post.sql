USE [cynewscorner]
GO

/****** Object:  Table [dbo].[post]    Script Date: 1/26/2021 12:31:44 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[post](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[title] [varchar](255) COLLATE SQL_Latin1_General_CP1253_CI_AI NOT NULL,
	[category] [varchar](255) COLLATE SQL_Latin1_General_CP1253_CI_AI NULL,
	[description] [text] COLLATE SQL_Latin1_General_CP1253_CI_AI NOT NULL,
	[url] [text] COLLATE SQL_Latin1_General_CP1253_CI_AI NOT NULL,
	[image] [varchar](255) NULL,
	[publish_datetime] [varchar](255) NULL,
	[added_on] [datetime] NULL
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


