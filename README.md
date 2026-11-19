# Microsserviço de Eventos Presenciais.

Um caso de uso para o estudo da arquitetura de microsserviços, a api permite que pequenos organizadores publiquem eventos e vendam ingressos para seus compradores.

# Configurar Variaveis Secretas

Para cada Microsserviço será necessário adicionar o `InternalApiKey`, abra cada microsserviço e execute o seguinte comando para ambientes locais:

```bash
dotnet user-secrets init
```

Após isso execute:

```bash
dotnet user-secrets set "InternalApi:ApiKey" "123456" 
```
