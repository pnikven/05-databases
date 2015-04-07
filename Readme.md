netsh http add urlacl url=http://+:15000/ user=Everyone listen=yes

###Презентации

* [Введение](https://github.com/konturschool/05-databases/raw/master/Презентации/Introduction.pdf)
* [Репликация и консистентность](https://github.com/konturschool/05-databases/raw/master/Презентации/Replication&Consistency.pptx)
* [Шардинг](https://github.com/konturschool/05-databases/raw/master/Презентации/Sharding.pdf)


###Домашнее задание

####Координатор
Нужно реализовать схему "Координатор".

Изменения нужно будет вносить в `ShardMappingController.cs` и `SimpleStorageClient.cs`.

Код координатора находится в проекте Coordinator. В `ShardMappingController.cs` нужно будет сделать так, чтобы функция `Get` возвращала номер шарды.

Для запуска координатора никакого скрипта нет, его можно запустить так:

```
\Coordinator\bin\Debug\Coordinator.exe -p 17000 -c 3
```

Для запуска шард нужно использовать скрипт `runAllShards.bat`, как на паре. Запускать нужно как на паре:

```
\SimpleStorage\bin\Debug\runAllShards.bat
```

В `SimpleStorageClient.cs` нужно реализовать следующий функционал:
1. Узнаем у координатора номер шарды. Это можно сделать при помощи `CoordinatorClient.cs` (его код находится в проекте `Client`). Поскольку в конфигурации `SimpleStorage` нет параметра для адреса координатора, приемлемым будет его создавать так:
    ```
    var coordinatorClient = new CoordinatorClient("http://127.0.0.1:17000");
    ```
2. Идем на шарду с этим номером.

Все проверяют тесты `/SimpleStorage.Tests/Sharding/Task3Tests.cs`, чтобы их запустить, нужно убрать атрибут `[Ignore]` (все как на паре).

####Quorum
Нужно реализовать схему QuorumRead-QuorumWrite.

Изменения нужно будет вносить в `ValuesController.cs` и `SimpleStorageClient.cs`.

В методах `Get` и `Put` в `ValuesController`, помимо операции с локальным `IStorage` нужно отправить аналогичную операцию на другие реплики и получить успешный ответ от Quorum узлов. Не забудьте выбрать самые актуальные данные в методе `Get`.
Для того чтобы избежать вечного цикла используйте `InternalClient` для общения между репликами.

В `SimpleStorageClient.cs` нужно реализовать попытки на случай если какая-то из реплик не доступна.

Запустить нужную конфигурацию кластера можно скриптом `runAll.bat`:

```
\SimpleStorage\bin\Debug\runAll.bat
```

Все проверяют тесты `/SimpleStorage.Tests/ReplicationAndConsistency/Task2Tests.cs`, чтобы их запустить, нужно убрать атрибут `[Ignore]` (все как на паре).
