USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Produto_Fiscal_UF_Interestadual]    Script Date: 25/04/2026 18:37:22 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Produto_Fiscal_UF_Interestadual](
	[UF] [char](2) NOT NULL,
	[CodigoProduto] [int] NOT NULL,
	[CodigoTributo] [tinyint] NOT NULL,
	[ReducaoICMS] [smallmoney] NULL,
	[AliquotaFCP] [smallmoney] NULL,
	[AliquotaICMSUFDestino] [smallmoney] NULL,
	[EmbasamentoLegal] [varchar](200) NULL,
 CONSTRAINT [PK_Produto_Fiscal_UF_Interestadual] PRIMARY KEY CLUSTERED 
(
	[UF] ASC,
	[CodigoProduto] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Produto_Fiscal_UF_Interestadual]  WITH CHECK ADD  CONSTRAINT [FK_Produto_Fiscal_UF_Interestadual_Tributo] FOREIGN KEY([CodigoTributo])
REFERENCES [dbo].[Produto_NCM_Tributo] ([CodigoTributo])
GO

ALTER TABLE [dbo].[Produto_Fiscal_UF_Interestadual] CHECK CONSTRAINT [FK_Produto_Fiscal_UF_Interestadual_Tributo]
GO

