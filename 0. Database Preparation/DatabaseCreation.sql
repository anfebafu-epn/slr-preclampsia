USE [DOC_Tmp3]
GO
/****** Object:  UserDefinedFunction [dbo].[RemoveNonAlphaCharacters]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Function [dbo].[RemoveNonAlphaCharacters](@Temp VarChar(4000))
Returns VarChar(4000)
AS
Begin

    Declare @KeepValues as varchar(50)
    Set @KeepValues = '%[^a-z0-9/.-[_]]%'
    While PatIndex(@KeepValues, @Temp)  > 0
        Set @Temp = Stuff(@Temp, PatIndex(@KeepValues, @Temp), 1, '') 

    Return @Temp
End
GO
/****** Object:  Table [dbo].[Author]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Author](
	[AuthorID] [uniqueidentifier] NOT NULL,
	[given] [varchar](250) NULL,
	[family] [varchar](250) NULL,
 CONSTRAINT [PK_BibEntryAuthor] PRIMARY KEY CLUSTERED 
(
	[AuthorID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BibEntry]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BibEntry](
	[EntryID] [uniqueidentifier] NOT NULL,
	[EntryType] [varchar](8000) NULL,
	[EntryKey] [varchar](8000) NULL,
	[abstract] [varchar](8000) NULL,
	[annote] [varchar](8000) NULL,
	[booktitle] [varchar](8000) NULL,
	[ISBN] [varchar](8000) NULL,
	[publisher] [varchar](8000) NULL,
	[title] [varchar](8000) NULL,
	[url] [varchar](8000) NULL,
	[year] [varchar](8000) NULL,
	[doi] [varchar](8000) NULL,
	[ISSN] [varchar](8000) NULL,
	[volume] [varchar](8000) NULL,
	[author] [varchar](8000) NULL,
	[keywords] [varchar](8000) NULL,
	[pages] [varchar](8000) NULL,
	[month] [varchar](8000) NULL,
	[journal] [varchar](8000) NULL,
	[number] [varchar](8000) NULL,
	[address] [varchar](8000) NULL,
	[editor] [varchar](8000) NULL,
	[archivePrefix] [varchar](8000) NULL,
	[arxivId] [varchar](8000) NULL,
	[eprint] [varchar](8000) NULL,
	[series] [varchar](8000) NULL,
	[db] [varchar](1000) NULL,
	[edition] [varchar](1000) NULL,
	[day] [varchar](100) NULL,
	[note] [varchar](1000) NULL,
	[EntryStatus] [int] NULL,
	[jsondata] [varchar](max) NULL,
	[indexed] [datetime] NULL,
	[publishedprint] [datetime] NULL,
	[articletype] [varchar](250) NULL,
	[created] [datetime] NULL,
	[score] [int] NULL,
	[issued] [datetime] NULL,
	[published] [datetime] NULL,
	[isreferencedbycount] [int] NULL,
	[cISSN] [varchar](100) NULL,
	[cISBN] [varchar](100) NULL,
	[containertitle] [varchar](1000) NULL,
	[bibtex] [varchar](max) NULL,
	[articleno] [varchar](250) NULL,
	[numpages] [varchar](250) NULL,
	[location] [varchar](250) NULL,
	[issue_date] [varchar](250) NULL,
 CONSTRAINT [PK__BibEntry__F57BD2D78601FE55] PRIMARY KEY CLUSTERED 
(
	[EntryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BibEntryAuthor]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BibEntryAuthor](
	[BibEntryAuthorID] [uniqueidentifier] NOT NULL,
	[AuthorID] [uniqueidentifier] NULL,
	[EntryID] [uniqueidentifier] NULL,
	[sequence] [varchar](250) NULL,
 CONSTRAINT [PK_BibEntryAuthor_1] PRIMARY KEY CLUSTERED 
(
	[BibEntryAuthorID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BibEntryEvent]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BibEntryEvent](
	[BibEntryEventID] [uniqueidentifier] NOT NULL,
	[EventID] [uniqueidentifier] NULL,
	[EntryID] [uniqueidentifier] NULL,
 CONSTRAINT [PK_BibEntryEvent] PRIMARY KEY CLUSTERED 
(
	[BibEntryEventID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BibEntryReference]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BibEntryReference](
	[BibEntryReferenceID] [uniqueidentifier] NOT NULL,
	[EntryID1] [uniqueidentifier] NULL,
	[EntryID2] [uniqueidentifier] NULL,
 CONSTRAINT [PK_BibEntryReference] PRIMARY KEY CLUSTERED 
(
	[BibEntryReferenceID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BibEntrySubject]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BibEntrySubject](
	[BibEntrySubjectID] [uniqueidentifier] NOT NULL,
	[EntryID] [uniqueidentifier] NULL,
	[SubjectID] [uniqueidentifier] NULL,
 CONSTRAINT [PK_BibEntrySubject] PRIMARY KEY CLUSTERED 
(
	[BibEntrySubjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EntryTag]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EntryTag](
	[EntryTagID] [uniqueidentifier] NOT NULL,
	[EntryID] [uniqueidentifier] NULL,
	[TAG] [varchar](200) NULL,
 CONSTRAINT [PK_EntryTag] PRIMARY KEY CLUSTERED 
(
	[EntryTagID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Event]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Event](
	[EventID] [uniqueidentifier] NOT NULL,
	[Name] [varchar](1000) NULL,
	[Location] [varchar](250) NULL,
	[Acronym] [varchar](250) NULL,
	[Generic] [varchar](250) NULL,
 CONSTRAINT [PK_Event] PRIMARY KEY CLUSTERED 
(
	[EventID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Subject]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Subject](
	[SubjectID] [uniqueidentifier] NOT NULL,
	[SubjectName] [varchar](500) NULL,
 CONSTRAINT [PK_Subject] PRIMARY KEY CLUSTERED 
(
	[SubjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[taggroup]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[taggroup](
	[tag] [varchar](200) NOT NULL,
	[taggroup] [varchar](200) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[tag] ASC,
	[taggroup] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[BibEntryAuthor]  WITH CHECK ADD  CONSTRAINT [FK_BibEntryAuthor_Author] FOREIGN KEY([AuthorID])
REFERENCES [dbo].[Author] ([AuthorID])
GO
ALTER TABLE [dbo].[BibEntryAuthor] CHECK CONSTRAINT [FK_BibEntryAuthor_Author]
GO
ALTER TABLE [dbo].[BibEntryAuthor]  WITH CHECK ADD  CONSTRAINT [FK_BibEntryAuthor_BibEntry] FOREIGN KEY([EntryID])
REFERENCES [dbo].[BibEntry] ([EntryID])
GO
ALTER TABLE [dbo].[BibEntryAuthor] CHECK CONSTRAINT [FK_BibEntryAuthor_BibEntry]
GO
ALTER TABLE [dbo].[BibEntryEvent]  WITH CHECK ADD  CONSTRAINT [FK_BibEntryEvent_BibEntry] FOREIGN KEY([EntryID])
REFERENCES [dbo].[BibEntry] ([EntryID])
GO
ALTER TABLE [dbo].[BibEntryEvent] CHECK CONSTRAINT [FK_BibEntryEvent_BibEntry]
GO
ALTER TABLE [dbo].[BibEntryEvent]  WITH CHECK ADD  CONSTRAINT [FK_BibEntryEvent_Event] FOREIGN KEY([EventID])
REFERENCES [dbo].[Event] ([EventID])
GO
ALTER TABLE [dbo].[BibEntryEvent] CHECK CONSTRAINT [FK_BibEntryEvent_Event]
GO
ALTER TABLE [dbo].[BibEntryReference]  WITH CHECK ADD  CONSTRAINT [FK_BibEntryReference_BibEntry] FOREIGN KEY([EntryID1])
REFERENCES [dbo].[BibEntry] ([EntryID])
GO
ALTER TABLE [dbo].[BibEntryReference] CHECK CONSTRAINT [FK_BibEntryReference_BibEntry]
GO
ALTER TABLE [dbo].[BibEntryReference]  WITH CHECK ADD  CONSTRAINT [FK_BibEntryReference_BibEntry1] FOREIGN KEY([EntryID2])
REFERENCES [dbo].[BibEntry] ([EntryID])
GO
ALTER TABLE [dbo].[BibEntryReference] CHECK CONSTRAINT [FK_BibEntryReference_BibEntry1]
GO
ALTER TABLE [dbo].[BibEntrySubject]  WITH CHECK ADD  CONSTRAINT [FK_BibEntrySubject_BibEntry] FOREIGN KEY([EntryID])
REFERENCES [dbo].[BibEntry] ([EntryID])
GO
ALTER TABLE [dbo].[BibEntrySubject] CHECK CONSTRAINT [FK_BibEntrySubject_BibEntry]
GO
ALTER TABLE [dbo].[BibEntrySubject]  WITH CHECK ADD  CONSTRAINT [FK_BibEntrySubject_Subject] FOREIGN KEY([SubjectID])
REFERENCES [dbo].[Subject] ([SubjectID])
GO
ALTER TABLE [dbo].[BibEntrySubject] CHECK CONSTRAINT [FK_BibEntrySubject_Subject]
GO
ALTER TABLE [dbo].[EntryTag]  WITH CHECK ADD  CONSTRAINT [FK_EntryTag_BibEntry] FOREIGN KEY([EntryID])
REFERENCES [dbo].[BibEntry] ([EntryID])
GO
ALTER TABLE [dbo].[EntryTag] CHECK CONSTRAINT [FK_EntryTag_BibEntry]
GO
/****** Object:  StoredProcedure [dbo].[BibEntry_Duplicados]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create procedure [dbo].[BibEntry_Duplicados] as

-- Limpia de espacios el doi
update bibentry
set doi = [dbo].[RemoveNonAlphaCharacters](doi)

-- Descarta los artículos sin doi
update bibentry
set EntryStatus = 11
where doi is null or ltrim(rtrim(doi)) = '' or [dbo].[RemoveNonAlphaCharacters](doi) = ''

-- si el doi inicia con url, se lo normaliza
update bibentry
set doi = replace(replace(replace(doi, 'https://doi.org/', ''), 'https//doiorg/', ''),'https//doi.org/','')

-- Retira duplicados comparando por doi
update bibentry
set EntryStatus = 12
from bibentry be
inner join (
select ROW_NUMBER() OVER (PARTITION BY doi ORDER BY case when db = 'Scopus' then 1 when abstract is null then 3 else 4 end) as num, entryid
from BibEntry
where entrystatus =1
) c on be.entryid = c.entryid and c.num > 1
where be.EntryStatus = 1


-- Retira duplicados comparando por título
update bibentry
set EntryStatus = 13
from bibentry be
inner join (
select ROW_NUMBER() OVER (PARTITION BY title ORDER BY case when db = 'Scopus' then 1 when abstract is null then 3 else 4 end) as num, entryid
from BibEntry
where entrystatus =1
) c on be.entryid = c.entryid and c.num > 1
where be.EntryStatus = 1

-- La búsqueda de abstract se hará después. Si no tiene abstract, se hará enriquecimiento para encontrarlo. Sino lo eliminará.

-- llena los campos null con la información de los inactivados
update b1
set EntryType = case when b1.EntryType is null and b2.EntryType is not null then b2.EntryType else b1.EntryType end,
EntryKey = case when b1.EntryKey is null and b2.EntryKey is not null then b2.EntryKey else b1.EntryKey end,
abstract = case when b1.abstract is null and b2.abstract is not null then b2.abstract else b1.abstract end,
annote = case when b1.annote is null and b2.annote is not null then b2.annote else b1.annote end,
booktitle = case when b1.booktitle is null and b2.booktitle is not null then b2.booktitle else b1.booktitle end,
ISBN = case when b1.ISBN is null and b2.ISBN is not null then b2.ISBN else b1.ISBN end,
publisher = case when b1.publisher is null and b2.publisher is not null then b2.publisher else b1.publisher end,
title = case when b1.title is null and b2.title is not null then b2.title else b1.title end,
url = case when b1.url is null and b2.url is not null then b2.url else b1.url end,
year = case when b1.year is null and b2.year is not null then b2.year else b1.year end,
doi = case when b1.doi is null and b2.doi is not null then b2.doi else b1.doi end,
ISSN = case when b1.ISSN is null and b2.ISSN is not null then b2.ISSN else b1.ISSN end,
volume = case when b1.volume is null and b2.volume is not null then b2.volume else b1.volume end,
author = case when b1.author is null and b2.author is not null then b2.author else b1.author end,
keywords = case when b1.keywords is null and b2.keywords is not null then b2.keywords else b1.keywords end,
pages = case when b1.pages is null and b2.pages is not null then b2.pages else b1.pages end,
month = case when b1.month is null and b2.month is not null then b2.month else b1.month end,
journal = case when b1.journal is null and b2.journal is not null then b2.journal else b1.journal end,
number = case when b1.number is null and b2.number is not null then b2.number else b1.number end,
address = case when b1.address is null and b2.address is not null then b2.address else b1.address end,
editor = case when b1.editor is null and b2.editor is not null then b2.editor else b1.editor end,
archivePrefix = case when b1.archivePrefix is null and b2.archivePrefix is not null then b2.archivePrefix else b1.archivePrefix end,
arxivId = case when b1.arxivId is null and b2.arxivId is not null then b2.arxivId else b1.arxivId end,
eprint = case when b1.eprint is null and b2.eprint is not null then b2.eprint else b1.eprint end,
series = case when b1.series is null and b2.series is not null then b2.series else b1.series end,
db = case when b1.db is null and b2.db is not null then b2.db else b1.db end,
edition = case when b1.edition is null and b2.edition is not null then b2.edition else b1.edition end,
day = case when b1.day is null and b2.day is not null then b2.day else b1.day end,
note = case when b1.note is null and b2.note is not null then b2.note else b1.note end,
jsondata = case when b1.jsondata is null and b2.jsondata is not null then b2.jsondata else b1.jsondata end,
articletype = case when b1.articletype is null and b2.articletype is not null then b2.articletype else b1.articletype end,
cISSN = case when b1.cISSN is null and b2.cISSN is not null then b2.cISSN else b1.cISSN end,
cISBN = case when b1.cISBN is null and b2.cISBN is not null then b2.cISBN else b1.cISBN end,
containertitle = case when b1.containertitle is null and b2.containertitle is not null then b2.containertitle else b1.containertitle end,
bibtex = case when b1.bibtex is null and b2.bibtex is not null then b2.bibtex else b1.bibtex end,
articleno = case when b1.articleno is null and b2.articleno is not null then b2.articleno else b1.articleno end,
numpages = case when b1.numpages is null and b2.numpages is not null then b2.numpages else b1.numpages end,
location = case when b1.location is null and b2.location is not null then b2.location else b1.location end,
issue_date = case when b1.issue_date is null and b2.issue_date is not null then b2.issue_date else b1.issue_date end

from bibentry b1
inner join bibentry b2 on b1.doi = b2.doi and b1.entrystatus = 1 and b2.entrystatus != 1
GO
/****** Object:  StoredProcedure [dbo].[Doc_Tags]    Script Date: 14/10/2023 23:24:58 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- análisis de coocurrencia
CREATE PROCEDURE [dbo].[Doc_Tags]
as
select * from (
select tagv, tagw, cty, len(tagv) + len(tagw) + cty as hsh, row_number () over (partition by len(tagv) + len(tagw) + cty order by len(tagv) + len(tagw) + cty) as num
--select '[''' + x.tagv + ' ' + x.tagw + '''],'
from
(
select v.tagv, w.tagw, 
(
	select count(*) from BibEntry be where 
	(select count(*) from entrytag et inner join taggroup tg on tg.tag = et.TAG where et.entryid = be.entryid and tg.tag = v.tagv) > 0 and
	(select count(*) from entrytag et inner join taggroup tg on tg.tag = et.TAG where et.entryid = be.entryid and tg.tag = w.tagw) > 0 and
	be.EntryStatus = 1
) as cty
from
( select distinct vtg.taggroup as tagv from entrytag ev inner join taggroup vtg on ev.TAG = vtg.tag) as v
cross join 
(select distinct wtg.taggroup as tagw from entrytag ew inner join taggroup wtg on ew.TAG = wtg.tag) as w
where v.tagv <> w.tagw
) as x
where x.cty >=3
--order by x.cty desc, x.tagv, x.tagw
) as iv
where iv.num = 1
GO
