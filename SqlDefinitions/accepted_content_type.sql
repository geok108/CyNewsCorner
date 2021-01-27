CREATE TABLE [dbo].[accepted_content_type](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[content_type] [varchar](45) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

insert into accepted_content_type values('application/xml')
insert into accepted_content_type values('text/xml; charset=UTF-8')
insert into accepted_content_type values('application/xml; charset=utf-8')
insert into accepted_content_type values('application/rss+xml; charset=utf-8')