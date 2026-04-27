USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Promocao_Empresa]    Script Date: 25/04/2026 18:39:17 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Promocao_Empresa](
	[CodigoPromocaoID] [int] NOT NULL,
	[CodigoPromocao] [int] NOT NULL,
	[CodigoEmpresa] [smallint] NOT NULL,
 CONSTRAINT [PK_Promocao_Empresa] PRIMARY KEY CLUSTERED 
(
	[CodigoPromocaoID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Promocao_Empresa]  WITH CHECK ADD  CONSTRAINT [FK_Promocao_Empresa_Promocao] FOREIGN KEY([CodigoPromocao])
REFERENCES [dbo].[Promocao] ([CodigoPromocao])
GO

ALTER TABLE [dbo].[Promocao_Empresa] CHECK CONSTRAINT [FK_Promocao_Empresa_Promocao]
GO

