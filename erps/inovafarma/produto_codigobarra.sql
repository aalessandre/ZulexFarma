USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Produto_CodigoBarra]    Script Date: 25/04/2026 18:38:01 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Produto_CodigoBarra](
	[CodigoProduto] [int] NOT NULL,
	[CodigoBarra] [varchar](15) NOT NULL,
	[CodigoBarraID] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_Produto_CodigoBarra] PRIMARY KEY CLUSTERED 
(
	[CodigoProduto] ASC,
	[CodigoBarra] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Produto_CodigoBarra] ADD  CONSTRAINT [DF_Produto_CodigoBarra_CodigoBarraID]  DEFAULT (newsequentialid()) FOR [CodigoBarraID]
GO

ALTER TABLE [dbo].[Produto_CodigoBarra]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_CodigoBarra_Produto] FOREIGN KEY([CodigoProduto])
REFERENCES [dbo].[Produto] ([CodigoProduto])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_CodigoBarra] CHECK CONSTRAINT [FK_Produto_CodigoBarra_Produto]
GO

