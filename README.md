# Pulse Widget

Widget leve para Windows que monitora CPU, GPU, memoria, armazenamento e ventoinhas. A interface usa WPF, graficos com buffer circular fixo e sensores fornecidos pelo LibreHardwareMonitor.

## Recursos

- Cards em tempo real para CPU, GPU e RAM.
- Graficos separados de uso e temperatura para CPU/GPU.
- Janela movel, redimensionavel, maximizavel e sempre no topo.
- Modo click-through e integracao com a bandeja do Windows.
- Intervalos de coleta de 1, 2 ou 5 segundos.
- Posicao e preferencias persistidas localmente.
- Icone vetorial de foguete gerado pelo proprio projeto.

## Requisitos

- Windows 10 ou Windows 11, x64
- .NET 8 SDK para desenvolvimento
- [PawnIO 2.2.0](https://github.com/namazso/PawnIO.Setup/releases/tag/2.2.0) para sensores de baixo nivel

O manifesto solicita administrador porque temperatura, clock e energia da CPU exigem acesso ao PawnIO. Nao desative a lista de drivers vulneraveis nem a seguranca do Windows para executar o widget.

## Executar durante o desenvolvimento

```powershell
dotnet restore
dotnet run
```

## Gerar versao portatil

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
```

O executavel sera criado em `dist\PulseWidget.exe`.

Para gerar o pacote self-contained usado pelo instalador:

```powershell
.\tools\Publish.ps1
```

O instalador usa Inno Setup 6 e o script `Installer\PulseWidget.iss`. A assinatura e opcional: defina `PULSE_SIGN_CERT_SHA1` com o thumbprint de um certificado instalado antes de executar `tools\Publish.ps1`. Certificados e chaves privadas nunca devem entrar no repositorio.

## Uso

- Arraste o cabecalho para posicionar o widget.
- Use o botao entre minimizar e fechar para maximizar ou restaurar.
- Um clique duplo no cabecalho tambem maximiza ou restaura.
- `PIN` alterna o modo sempre no topo.
- `_` ou `X` ocultam o widget na bandeja do Windows.
- Clique duas vezes no icone da bandeja para mostrar o widget.
- O menu da bandeja controla click-through, intervalo de atualizacao, inicio com o Windows e encerramento.
- Quando o modo `Ignorar cliques` estiver ativo, use o menu da bandeja para desativa-lo.

As configuracoes sao salvas em `%LOCALAPPDATA%\PulseWidget\settings.json`.

## Desempenho

- Coleta padrao: uma leitura por segundo.
- Discos, placa-mae e ventoinhas sao atualizados a cada cinco leituras.
- Historico: 120 pontos em graficos separados de uso e temperatura de CPU/GPU.
- O LibreHardwareMonitor e atualizado fora da thread da interface.
- Nao ha banco de dados, telemetria ou animacoes continuas.

No teste local de 140 segundos, o processo permaneceu aberto, usou aproximadamente 159 MB de memoria privada apos preencher os graficos e consumiu cerca de 2,7% de um unico nucleo. Os resultados variam conforme hardware, quantidade de sensores e versao do Windows.

## Limitacoes do MVP

- Os sensores disponiveis dependem da placa-mae e dos drivers.
- FPS e frametime ainda nao fazem parte do widget.
- O widget nao injeta codigo em jogos. Uma futura versao pode integrar com RTSS para o OSD.

## Licenca

O Pulse Widget usa a licenca MIT. Dependencias possuem suas proprias licencas; consulte `THIRD_PARTY_NOTICES.md`.
