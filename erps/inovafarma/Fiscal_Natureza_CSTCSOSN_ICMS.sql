USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Fiscal_Natureza_CSTCSOSN_ICMS]    Script Date: 26/04/2026 05:34:54 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Fiscal_Natureza_CSTCSOSN_ICMS](
	[CodigoNatureza] [smallint] NOT NULL,
	[CSTTributadoSemInscricaoEstadualDE] [varchar](2) NULL,
	[CSTTributadoSemInscricaoEstadualFE] [varchar](2) NULL,
	[CSTTributadoPessoaJuridicaComInscricaoEstadualDE] [varchar](2) NULL,
	[CSTTributadoPessoaJuridicaComInscricaoEstadualFE] [varchar](2) NULL,
	[CSTSubstituicaoTributariaDE] [varchar](2) NULL,
	[CSTSubstituicaoTributariaFE] [varchar](2) NULL,
	[CSTIsentoDE] [varchar](2) NULL,
	[CSTIsentoFE] [varchar](2) NULL,
	[CSTNaoTributadoDE] [varchar](2) NULL,
	[CSTNaoTributadoFE] [varchar](2) NULL,
	[CSTOutrosDE] [varchar](2) NULL,
	[CSTOutrosFE] [varchar](2) NULL,
	[CSTParaDocumentoReferenciadoDE] [varchar](2) NULL,
	[CSTParaDocumentoReferenciadoFE] [varchar](2) NULL,
	[CSOSNTributadoSemInscricaoEstadualDE] [varchar](3) NULL,
	[CSOSNTributadoPessoaJuridicaComInscricaoEstadualDE] [varchar](3) NULL,
	[CSOSNSubstituicaoTributariaDE] [varchar](3) NULL,
	[CSOSNIsentoDE] [varchar](3) NULL,
	[CSOSNNaoTributadoDE] [varchar](3) NULL,
	[CSOSNOutrosDE] [varchar](3) NULL,
	[CSOSNParaDocumentoReferenciadoDE] [varchar](3) NULL,
	[CSOSNTributadoSemInscricaoEstadualFE] [varchar](3) NULL,
	[CSOSNTributadoPessoaJuridicaComInscricaoEstadualFE] [varchar](3) NULL,
	[CSOSNSubstituicaoTributariaFE] [varchar](3) NULL,
	[CSOSNIsentoFE] [varchar](3) NULL,
	[CSOSNNaoTributadoFE] [varchar](3) NULL,
	[CSOSNOutrosFE] [varchar](3) NULL,
	[CSOSNParaDocumentoReferenciadoFE] [varchar](3) NULL,
	[rowguid] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
 CONSTRAINT [PK_Fiscal_Natureza_CSTCSOSN_ICMS] PRIMARY KEY CLUSTERED 
(
	[CodigoNatureza] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Fiscal_Natureza_CSTCSOSN_ICMS] ADD  CONSTRAINT [DF__Fiscal_Na__rowgu__1FDE3944]  DEFAULT (newsequentialid()) FOR [rowguid]
GO

ALTER TABLE [dbo].[Fiscal_Natureza_CSTCSOSN_ICMS]  WITH NOCHECK ADD  CONSTRAINT [FK_Fiscal_Natureza_Fiscal_Natureza_CSTCSOSN_ICMS] FOREIGN KEY([CodigoNatureza])
REFERENCES [dbo].[Fiscal_Natureza] ([CodigoNatureza])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Fiscal_Natureza_CSTCSOSN_ICMS] CHECK CONSTRAINT [FK_Fiscal_Natureza_Fiscal_Natureza_CSTCSOSN_ICMS]
GO

