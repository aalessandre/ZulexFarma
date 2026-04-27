USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Produto_NCM_Tributo]    Script Date: 25/04/2026 18:54:41 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Produto_NCM_Tributo](
	[CodigoTributo] [tinyint] NOT NULL,
	[NomeTributo] [varchar](50) NOT NULL,
	[CodigoTabela] [varchar](3) NOT NULL,
	[CodigoTabelaConvenio] [varchar](3) NOT NULL,
	[CSOSN] [varchar](3) NULL,
 CONSTRAINT [PK_Produto_NCM_Tributo] PRIMARY KEY CLUSTERED 
(
	[CodigoTributo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

