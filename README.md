# Pulse Widget

Widget leve para Windows que monitora CPU, GPU, memoria, armazenamento, ventoinhas, rede e, opcionalmente, FPS. A interface usa WPF, historico limitado por tempo e sensores fornecidos pelo LibreHardwareMonitor.

## Recursos

- Cards em tempo real para CPU, GPU e RAM.
- Graficos separados de uso e temperatura para CPU/GPU.
- Historico configuravel de 2, 5, 15 ou 30 minutos com minimo, media e maximo.
- Alertas sustentados de temperatura com histerese e cooldown.
- VRAM, SSD, ventoinhas, download e upload identificados por dispositivo.
- Selecao estavel de GPU em computadores com mais de uma placa.
- Temas escuro, grafite e claro, opacidade e metricas configuraveis.
- Modos completo e compacto.
- Janela movel, redimensionavel, maximizavel e sempre no topo.
- Modo click-through e integracao com a bandeja do Windows.
- Intervalos de coleta de 1, 2 ou 5 segundos.
- Diagnostico copiavel/exportavel e log rotativo limitado a 1 MB.
- FPS e frametime opcionais pela memoria compartilhada do RTSS.
- Instancia unica: novas aberturas ativam o widget existente.
- Posicao e preferencias persistidas localmente.
- Icone vetorial de foguete gerado pelo proprio projeto.

## Requisitos

- Windows 10 ou Windows 11, x64
- .NET 8 SDK para desenvolvimento
- [PawnIO 2.2.0](https://github.com/namazso/PawnIO.Setup/releases/tag/2.2.0) para sensores de baixo nivel

A interface roda sem elevacao. Ao iniciar a coleta, uma instancia auxiliar do mesmo executavel solicita UAC somente para temperatura, clock, energia e outros sensores protegidos pelo PawnIO. Recusar o UAC mantem a interface aberta em modo degradado. Nao desative a lista de drivers vulneraveis nem a seguranca do Windows.

## Executar durante o desenvolvimento

```powershell
dotnet restore
dotnet run --project PulseWidget.csproj
```

Execute os testes com `dotnet test PulseWidget.sln -c Release`.

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
- `SET` abre configuracoes de tema, GPU, historico, alertas e metricas.
- `CMP` alterna o modo compacto.
- O menu `Diagnostico` mostra sensores, identificadores, valores, RTSS e logs recentes.
- Quando o modo `Ignorar cliques` estiver ativo, use o menu da bandeja para desativa-lo.

As configuracoes sao salvas em `%LOCALAPPDATA%\PulseWidget\settings.json`.
O log fica em `%LOCALAPPDATA%\PulseWidget\pulse-widget.log`.

## Desempenho

- Coleta padrao: uma leitura por segundo.
- Discos, placa-mae e ventoinhas sao atualizados a cada cinco leituras.
- Historico limitado pela janela de tempo escolhida, com teto defensivo de 10.000 amostras.
- O LibreHardwareMonitor e atualizado fora da thread da interface.
- A interface e o coletor elevado comunicam-se por named pipe autenticado com token aleatorio.
- No autostart, a interface fica na bandeja e nao solicita UAC ate o widget ser aberto.
- Nao ha banco de dados, telemetria ou animacoes continuas.

No teste local de 140 segundos, o processo permaneceu aberto, usou aproximadamente 159 MB de memoria privada apos preencher os graficos e consumiu cerca de 2,7% de um unico nucleo. Os resultados variam conforme hardware, quantidade de sensores e versao do Windows.

## Limitacoes do MVP

- Os sensores disponiveis dependem da placa-mae e dos drivers.
- FPS e frametime sao opcionais e exigem o RivaTuner Statistics Server em execucao.
- O widget apenas le a memoria compartilhada do RTSS e nao injeta codigo por conta propria.
- Assinatura Authenticode exige um certificado externo; o pipeline possui apenas o hook seguro para assinatura.
- PawnIO e RTSS sao dependencias externas e nao sao redistribuidos pelo projeto.

## Automacao

- `CI`: compila, testa, audita pacotes e publica o artefato Windows x64.
- `Dependabot`: verifica NuGet e GitHub Actions semanalmente.
- Tags `v*`: geram uma GitHub Release com o pacote completo.

## Licenca

O Pulse Widget usa a licenca MIT. Dependencias possuem suas proprias licencas; consulte `THIRD_PARTY_NOTICES.md`.
