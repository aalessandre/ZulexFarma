USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Fiscal_Produto]    Script Date: 26/04/2026 05:34:13 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Fiscal_Produto](
	[CodigoMercadoria] [bigint] NOT NULL,
	[CodigoFiscal] [bigint] NOT NULL,
	[CodigoProduto] [int] NOT NULL,
	[Quantidade] [numeric](8, 2) NOT NULL,
	[Preco] [money] NOT NULL,
	[Total] [money] NOT NULL,
	[ICMS] [smallmoney] NOT NULL,
	[CodigoTributo] [tinyint] NOT NULL,
	[Desconto] [money] NOT NULL,
	[CodigoNatureza] [smallint] NULL,
	[IPI] [smallmoney] NULL,
	[CFOP] [varchar](5) NULL,
	[BaseIPI] [money] NULL,
	[BaseICMS] [money] NULL,
	[BasePis] [money] NULL,
	[Pis] [money] NULL,
	[BaseCofins] [money] NULL,
	[Cofins] [money] NULL,
	[Frete] [money] NULL,
	[Outro] [money] NULL,
	[IVA] [smallmoney] NULL,
	[BaseII] [money] NULL,
	[II] [money] NULL,
	[Tributos] [money] NULL,
	[BaseICMSST] [money] NULL,
	[ICMSST] [money] NULL,
	[ICMSSTPagar] [money] NULL,
	[AliquotaICMSAproveitado] [money] NULL,
	[CreditoICMSAproveitado] [money] NULL,
	[CST] [varchar](3) NULL,
	[Lote] [varchar](30) NULL,
	[DataValidade] [smalldatetime] NULL,
	[DataFabricacao] [smalldatetime] NULL,
	[TributosFederal] [smallmoney] NOT NULL,
	[TributosEstadual] [smallmoney] NOT NULL,
	[TributosMunicipal] [smallmoney] NOT NULL,
	[CST_PisCofins] [varchar](2) NULL,
	[AliquotaICMSST] [smallmoney] NOT NULL,
	[NCM] [varchar](10) NULL,
	[AliquotaFCP] [smallmoney] NOT NULL,
	[TotalFCP] [money] NOT NULL,
	[BaseFCP] [money] NOT NULL,
	[BaseFCPST] [money] NOT NULL,
	[AliquotaFCPST] [smallmoney] NOT NULL,
	[TotalFCPST] [money] NOT NULL,
	[RegistroMS] [varchar](13) NULL,
	[InformacaoAdicionalProduto] [varchar](500) NULL,
	[CEST] [varchar](7) NULL,
	[NumeroPedido] [varchar](15) NULL,
	[NumeroItemPedido] [varchar](15) NULL,
	[AliquotaICMS] [smallmoney] NULL,
	[PercentualRedBCEfetivo] [money] NULL,
	[ValorBCEfetivo] [money] NULL,
	[PercentualICMSEfetivo] [money] NULL,
	[ValorICMSEfetivo] [money] NULL,
	[ValorBCSTRetido] [money] NULL,
	[PercentualST] [money] NULL,
	[ValorICMSSTRetido] [money] NULL,
	[CodigoBeneficio] [varchar](8) NULL,
	[PisCofinsNatureza] [varchar](3) NULL,
	[ICMS_Valor] [money] NULL,
	[TotalIPI] [money] NULL,
	[PIS_Aliquota] [smallmoney] NULL,
	[COFINS_Aliquota] [smallmoney] NULL,
	[Fracao] [smallint] NOT NULL,
	[ICMS_PorcentagemDiferimento] [smallmoney] NULL,
	[ICMS_ValorDiferimento] [money] NULL,
	[Origem] [tinyint] NULL,
	[ReducaoICMS] [smallmoney] NULL,
	[ICMS_ValorOperacao] [money] NULL,
	[ICMS_ValorSubstituto] [money] NULL,
	[ReducaoICMSST] [smallmoney] NULL,
	[CST_IPI] [varchar](2) NULL,
	[ICMS_ValorDesonerado] [money] NULL,
	[Seguro] [money] NULL,
 CONSTRAINT [PK_Fiscal_Mercadoria] PRIMARY KEY CLUSTERED 
(
	[CodigoMercadoria] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Fiscal_Produto] ADD  CONSTRAINT [DF_Fiscal_Produto_Desconto]  DEFAULT ((0)) FOR [Desconto]
GO

ALTER TABLE [dbo].[Fiscal_Produto] ADD  CONSTRAINT [DF__Fiscal_Pr__Tribu__19418644]  DEFAULT ((0)) FOR [TributosFederal]
GO

ALTER TABLE [dbo].[Fiscal_Produto] ADD  CONSTRAINT [DF__Fiscal_Pr__Tribu__1A35AA7D]  DEFAULT ((0)) FOR [TributosEstadual]
GO

ALTER TABLE [dbo].[Fiscal_Produto] ADD  CONSTRAINT [DF__Fiscal_Pr__Tribu__1B29CEB6]  DEFAULT ((0)) FOR [TributosMunicipal]
GO

ALTER TABLE [dbo].[Fiscal_Produto] ADD  CONSTRAINT [DF__Fiscal_Pr__Aliqu__66F5FD99]  DEFAULT ((0)) FOR [AliquotaICMSST]
GO

ALTER TABLE [dbo].[Fiscal_Produto] ADD  CONSTRAINT [DF_Fiscal_Produto_AliquotaFCP]  DEFAULT ((0)) FOR [AliquotaFCP]
GO

ALTER TABLE [dbo].[Fiscal_Produto] ADD  CONSTRAINT [DF_Fiscal_Produto_TotalFCP]  DEFAULT ((0)) FOR [TotalFCP]
GO

ALTER TABLE [dbo].[Fiscal_Produto] ADD  CONSTRAINT [DF_Fiscal_Produto_BaseFCP]  DEFAULT ((0)) FOR [BaseFCP]
GO

ALTER TABLE [dbo].[Fiscal_Produto] ADD  CONSTRAINT [DF_Fiscal_Produto_BaseFCPST]  DEFAULT ((0)) FOR [BaseFCPST]
GO

ALTER TABLE [dbo].[Fiscal_Produto] ADD  CONSTRAINT [DF_Fiscal_Produto_AliquotaFCPST]  DEFAULT ((0)) FOR [AliquotaFCPST]
GO

ALTER TABLE [dbo].[Fiscal_Produto] ADD  CONSTRAINT [DF_Fiscal_Produto_TotalFCPST]  DEFAULT ((0)) FOR [TotalFCPST]
GO

ALTER TABLE [dbo].[Fiscal_Produto] ADD  CONSTRAINT [DF_Fiscal_Produto_Fracao]  DEFAULT ((0)) FOR [Fracao]
GO

ALTER TABLE [dbo].[Fiscal_Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Fiscal_Produto_Fiscal_Natureza] FOREIGN KEY([CodigoNatureza])
REFERENCES [dbo].[Fiscal_Natureza] ([CodigoNatureza])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Fiscal_Produto] CHECK CONSTRAINT [FK_Fiscal_Produto_Fiscal_Natureza]
GO

ALTER TABLE [dbo].[Fiscal_Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Fiscal_Produto_Produto_NCM_Tributo] FOREIGN KEY([CodigoTributo])
REFERENCES [dbo].[Produto_NCM_Tributo] ([CodigoTributo])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Fiscal_Produto] CHECK CONSTRAINT [FK_Fiscal_Produto_Produto_NCM_Tributo]
GO

ALTER TABLE [dbo].[Fiscal_Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Fiscal_Produtos_Fiscal] FOREIGN KEY([CodigoFiscal])
REFERENCES [dbo].[Fiscal] ([CodigoFiscal])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Fiscal_Produto] CHECK CONSTRAINT [FK_Fiscal_Produtos_Fiscal]
GO

ALTER TABLE [dbo].[Fiscal_Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Fiscal_Produtos_Produto] FOREIGN KEY([CodigoProduto])
REFERENCES [dbo].[Produto] ([CodigoProduto])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Fiscal_Produto] CHECK CONSTRAINT [FK_Fiscal_Produtos_Produto]
GO

