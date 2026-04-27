USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Promocao_DiaSemana]    Script Date: 25/04/2026 18:39:04 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Promocao_DiaSemana](
	[CodigoPromocao] [int] NOT NULL,
	[Dia] [tinyint] NOT NULL,
	[rowguid] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
 CONSTRAINT [PK_CodigoPromocao_Dia] PRIMARY KEY CLUSTERED 
(
	[CodigoPromocao] ASC,
	[Dia] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Promocao_DiaSemana] ADD  CONSTRAINT [DF__Promocao___rowgu__0925BC75]  DEFAULT (newsequentialid()) FOR [rowguid]
GO

ALTER TABLE [dbo].[Promocao_DiaSemana]  WITH CHECK ADD  CONSTRAINT [FK_PromocaoDiaSemana_Promocao] FOREIGN KEY([CodigoPromocao])
REFERENCES [dbo].[Promocao] ([CodigoPromocao])
GO

ALTER TABLE [dbo].[Promocao_DiaSemana] CHECK CONSTRAINT [FK_PromocaoDiaSemana_Promocao]
GO

