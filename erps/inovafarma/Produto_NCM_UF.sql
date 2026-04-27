USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Produto_NCM_UF]    Script Date: 25/04/2026 18:54:57 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Produto_NCM_UF](
	[CodigoNCMID] [bigint] NOT NULL,
	[UF] [char](2) NOT NULL,
	[NCM] [varchar](10) NOT NULL,
	[CodigoTributo] [tinyint] NOT NULL,
	[IcmsImportado] [smallmoney] NOT NULL,
	[ReducaoICMS] [smallmoney] NOT NULL,
	[IVA] [smallmoney] NOT NULL,
	[CodigoDecreto] [smallint] NULL,
	[ICMS] [smallmoney] NOT NULL,
 CONSTRAINT [PK_Produto_NCM_UF] PRIMARY KEY CLUSTERED 
(
	[CodigoNCMID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Produto_NCM_UF] ADD  CONSTRAINT [DF_Produto_NCM_UF_Icms]  DEFAULT ((0)) FOR [IcmsImportado]
GO

ALTER TABLE [dbo].[Produto_NCM_UF] ADD  CONSTRAINT [DF_Produto_NCM_UF_ReducaoICMS]  DEFAULT ((0)) FOR [ReducaoICMS]
GO

ALTER TABLE [dbo].[Produto_NCM_UF] ADD  CONSTRAINT [DF_Produto_NCM_UF_IVA]  DEFAULT ((0)) FOR [IVA]
GO

ALTER TABLE [dbo].[Produto_NCM_UF] ADD  CONSTRAINT [DF_Produto_NCM_UF_ICMSDiferenciado]  DEFAULT ((0)) FOR [ICMS]
GO

ALTER TABLE [dbo].[Produto_NCM_UF]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_NCM_UF_Produto_NCM] FOREIGN KEY([NCM])
REFERENCES [dbo].[Produto_NCM] ([NCM])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_NCM_UF] CHECK CONSTRAINT [FK_Produto_NCM_UF_Produto_NCM]
GO

ALTER TABLE [dbo].[Produto_NCM_UF]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_NCM_UF_Produto_NCM_Decreto] FOREIGN KEY([CodigoDecreto])
REFERENCES [dbo].[Produto_NCM_Decreto] ([CodigoDecreto])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_NCM_UF] CHECK CONSTRAINT [FK_Produto_NCM_UF_Produto_NCM_Decreto]
GO

ALTER TABLE [dbo].[Produto_NCM_UF]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_NCM_UF_Produto_NCM_Tributo] FOREIGN KEY([CodigoTributo])
REFERENCES [dbo].[Produto_NCM_Tributo] ([CodigoTributo])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_NCM_UF] CHECK CONSTRAINT [FK_Produto_NCM_UF_Produto_NCM_Tributo]
GO

