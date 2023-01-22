# Gongo

![License](https://img.shields.io/github/license/davidsonbsilva/gongo.svg) ![Status](https://img.shields.io/badge/status-stopped-red)

![Captura de tela do Gongo](cover.png)

[[Ver em Português](README.pt-br.md)]

**Gongo** é um simples chat construído em C# para fins de aprendizado sobre sockets durante a disciplina de Redes de Computadores na [Universidade Federal dos Vales do Jequitinhonha e Mucuri](http://portal.ufvjm.edu.br/).

Trata-se de uma aplicação provida de uma arquitetura cliente-servidor que permite que múltiplos usuários se comuniquem simultaneamente através do protocolo TCP. Até o momento, o único idioma suportado para **Gongo** é Português (Brasil).

## Requisitos

- .Net Framework 4.8
  
> O software foi testado em ambiente Windows. Caso tenha problemas com execução em outros ambientes, [entre em contato](#contato).

## Instalação

Clone o repositório:

```
$ git clone https://github.com/davidsonbrsilva/gongo.git
```

Acesse a pasta raiz do projeto:

```
cd gongo
```

Construa a aplicação:

```
dotnet build
```

## Guia rápido de uso

Há dois projetos executáveis na mesma solução, **Gongo Server** e **Gongo Client**. Inicie o arquivo `GongoServer.exe` para executar o servidor da aplicação. Em seguida, abra o arquivo `GongoClient.exe` para começar uma conversa.

_Gongo Client_ usará o seu IP local como nome de usuário por padrão, mas, você pode alterar isso a qualquer momento. Basta clicar em `(alterar)`, digitar o novo nome de usuário que deseja no campo de mensagem e clicar em `Confirmar`.

Mensagens enviadas por você são exibidas em roxo e mensagens enviadas por outros usuários são verde.

Você pode simular uma conversa entre múltiplos usuários iniciando e enviando mensagens por meio de mais de uma instância de _Gongo Client_.

## Arquivos de Configuração do Gongo

_Gongo Server_ e _Gongo Client_ procurarão por seus respectivos arquivos de configuração antes de iniciar a aplicação.

### Arquivo de configurações do Gongo Server

Por padrão, _Gongo Server_ receberá conexões de qualquer endereço de IP e as escutará na porta `22777`. Porém, você pode sobrescrever essas configurações.

Para isso, crie um arquivo chamado `ServerSettings.json` no mesmo diretório em que se encontra o arquivo executável do _Gongo Server_, com a seguinte estrutura:

```json
{
    "host":"any",
    "port":"<your_custom_port>"
}
```

Ao especificar `any` na propriedade `host`, você informa ao _Gongo Server_ que deseja receber conexões de qualquer endereço de IP. Você também pode informar um endereço de IP para que _Gongo Server_ receba conexões apenas deste em específico.

Lembre-se que se você usar uma porta diferente para o Gongo Server, as instâncias do _Gongo Client_ também devem se conectar na mesma porta liberada pelo servidor.

### Arquivo de configurações do Gongo Client

Por padrão, _Gongo Client_ considerará que _Gongo Server_ está rodando em sua máquina local e tentará se conectar ao seu IP na porta padrão do servidor (22777).

Para mudar isso, crie um arquivo de configurações no mesmo diretório do executável do _Gongo Client_ chamado `ClientSettings.json` com a seguinte estrutura:

```json
{
    "host":"<gongo_server_ip>",
    "port":"<gongo_server_port>"
}
```

## Contato

Caso necessite, envie um e-mail para <davidsonbruno@outlook.com>.

## Licença

[MIT](LICENSE.md) Copyright (c) 2019, Davidson Bruno.
