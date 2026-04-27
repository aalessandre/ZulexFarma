USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Promocao_Produto]    Script Date: 25/04/2026 18:39:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Promocao_Produto](
	[CodigoPromocaoID] [bigint] NOT NULL,
	[CodigoPromocao] [int] NOT NULL,
	[CodigoProduto] [int] NOT NULL,
	[PrecoPromocao] [money] NOT NULL,
	[Comissao] [smallmoney] NOT NULL,
	[Porcentagem] [smallmoney] NOT NULL,
	[QuantidadeLevar] [smallint] NULL,
	[QuantidadePagar] [smallint] NULL,
	[PrecoBruto] [money] NULL,
 CONSTRAINT [PK_Promocao_Produto] PRIMARY KEY CLUSTERED 
(
	[CodigoPromocaoID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Promocao_Produto] ADD  CONSTRAINT [DF_Promocao_Produto_Porcentagem]  DEFAULT ((0)) FOR [Porcentagem]
GO

ALTER TABLE [dbo].[Promocao_Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Promocao_Produto_Produto] FOREIGN KEY([CodigoProduto])
REFERENCES [dbo].[Produto] ([CodigoProduto])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Promocao_Produto] CHECK CONSTRAINT [FK_Promocao_Produto_Produto]
GO

ALTER TABLE [dbo].[Promocao_Produto]  WITH CHECK ADD  CONSTRAINT [FK_Promocao_Produto_Promocao] FOREIGN KEY([CodigoPromocao])
REFERENCES [dbo].[Promocao] ([CodigoPromocao])
GO

ALTER TABLE [dbo].[Promocao_Produto] CHECK CONSTRAINT [FK_Promocao_Produto_Promocao]
GO

