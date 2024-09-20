Primeira coisa, é necessário rodar a imagem do Docker Emulator localmente.
Basta ter o Docker CLI na máquina. Se você não tiver a imagem, esse procedimento já vai baixá-la e adicioná-la ao seu repositório local.
`
sudo docker run -d --name cosmos-emulator -p 8081:8081 -p 10250-10254:10250-10254 -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=3 -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator
`

Isso vai fazer ele rodar na sua porta 8081, no entanto, é preciso instalar o certificado para conseguir de comunicar com ele através de ssl.
Para isso, dois passos:
1) Salve o certificado na pasta raiz do usuário:
rodar no bash curl --insecure https://localhost:8081/_explorer/emulator.pem > ~/emulatorcert.crt
2) Para linux, copiar este certificado para pasta de cacerts:
rodar no bash sudo cp ~/emulatorcert.crt /usr/local/share/ca-certificates/
3) Para linux, fazer update no cacerts:
rodar no bash sudo update-ca-certificates

Feito isso, agora o Container com o Cosmos Emulator já está funcionando e a máquina consegue acessar.
O próximo passo é rodar a function. Abrir um console na pasta projeto da function.

Para rodar o func, é necessário usar node > 7. Portanto:
1) rodar no bash: nvm use 18.0.0;
2) rodar no bash: func start

Isso vai deployar a function na porta localhost:7071

Se fizer: http://localhost:7071/api/CheckClientStatus?cpf=00413032965 vai dar positivo, pois o código da function está mockando este cpf;
Se fizer: http://localhost:7071/api/CheckClientStatus?cpf=XXXXXXXXXXX vai dar negativo, pois o cpf não estará cadastrado no Cosmos.