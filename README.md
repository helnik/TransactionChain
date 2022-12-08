## Introduction

There are many cases where a transaction (action) within a code execution flow may consist of multiple (internal) sub-transactions which are required to run in a specific order. In such cases, it is quite often that one sub-transaction depends on the outcome of a previous executed one, and/or if one sub-transaction fails some or all of the previously executed ones need to perform a roll back. 

TransactionChain is a simple framework that aims to resolve such cases in a unified way. It is based on object oriented design where relationships between command chain and executor are defined by the framework and all the required interactions are constrained by abstraction of interfaces, thus providing a simple and streamlined pattern to implement required functionality.

## Basic Components

The components depicted in the below schema are encapsulated: 

![Components]( https://github.com/helnik/TransactionChain/blob/main/Solution%20Items/TransactionChainComponents.jpg)

-	**Executor**: Responsible for executing provided chain of commands in the specified requested order. In case one of the commands fails or gets canceled by the cancellation token, used in the ICommand interface, the executor is responsible for rolling back all executed transactions (given that rollback is provided) by calling the RollbackAsync method of each ICommand.
Implementing the business logic within the ExecutionAsync and RollbackAsync methods used by the executor is responsibility of the ICommand implementation.
    -	**ExecuteAsync**:  Executes the list of commands provided as parameter and returns the last command executed. Note that each command has a NextAsync() method which can insert the child   command into the execution queue, to be executed next after this command. So the total number of commands can be greater than provided in the commands parameter.
   
         ``` Task<ICommand> ExecuteAsync(IEnumerable<ICommand> commands, CancellationToken cancellationToken = default) ```  
    
    
    - **RollbackAsync**: Executes RollbackAsync of each command executed in **reverse order**. Each concrete ICommand implementation is responsible to provide implementation that reverts executed actions.
    
        ``` Task RollbackAsync() ```  
       
 - **ICommand**: Each concrete implementation is a worker responsible to execute the business logic  of the command within the ExecuteAsync method. Also responsible to provide RollbackAsync method implementation to reverts the work of the failed worker.
Each of the ICommand instances can return a derived instance when the NextAsync method is called by the Executor. The returned command will be executed immediately after the parent command.
When command encounters an exception during the execution it's up to the implementation to (i) either handle the exception, recover and proceed executing the business logic, or (ii) throw and let exception get handled by the executor. In the second case execution of whole commands chain will be interrupted and already executed commands will be rolled back. 

    -   **ExecuteAsync**: Executes the business logic associated with the command. CancellationToken can be used to check if the command execution chain is being canceled or timed out.

        ```Task ExecuteAsync(CancellationToken cancellationToken) ```

    - **RollbackAsync**: Reverts, if possible, everything that ExecuteAsync() executed. If roll back is not intended, execution can be returned without performing nothing. When roll back is required but not possible an exception should be thrown.    

        ``` Task RollbackAsync() ```

    - **NextAsync**:Returns the ICommand that follows the command executed. Current command can create and pass any context/parameters to the next ICommand

        ``` Task<ICommand> NextAsync() ```

- **ITypedCommand\<T\>**: Type command inherits the ICommand interface, providing additional ability to expose the command context, which could include for example the result of command execution.

    ``` 
    public interface ITypedCommand<out T> : ICommand
    {
            T Context { get; }
    }
    ```  
  Implementation can use the Context property, to return value as result of the command execution. The same context can be passed to the next action returned by the NextAsync.

  The Executor class has extension methods for executing the ITypedCommand instances, allowing it to get the context of the last action in the chain after its execution. 

  ``` 
  public static async Task<T> ExecuteAsync<T>(this IExecutor executor, 
      IEnumerable<ITypedCommand<T>> commands, 
      CancellationToken cancellationToken = default);
  ```

- **TransactionChainException**: Exposes two (2) properties:
1.	CommandsRollbacked: The number of commands that TransactionChain managed to rollback. In a happy rollback path this number will be equal to the rollback commands added to the chain.
2.	RevertException: If any exception occurs while rolling back it will be held by this property.

    TransactionChain user should always try to catch this exception **first** and then any other exception. The usage logic is: 

    1. If **no exception occurs** then the chain was **executed successfully**. 
    2. If **TransactionChainException occurs** then: 
        * If **RevertException _IS_ present** then **rollback did not succeed**. The **original exception** that stopped the execution is the **inner exception** and the **RevertException** is the one that **stopped the rollback** process. You can infer the rollback step that revert exception occurred from CommnadsRollbacked count.
        * If **RevertException _IS NOT_ present** then **rollback completed successfully**. The **original exception** that stopped the execution is the **inner exception** and CommnadsRollbacked count should much the one rollback commands added to the chain.

## Fluid Extension

Fluid is an extension API that allows simplified usage of TransactionChain. It allows TransactionChain user to avoid implementing ICommand or ITypedCommand interfaces, by providing generic implementations of these interfaces. It works using dynamic delegates.

### Sample of Usage: Fluid

```
try
{
    var result = 
        await CommandChain.Forge(async ct =>
            await DoAwesomeStuff("John", Doe"))
                .CanRollback(() => true) //Func returning bool. Checks if rollback should be executed. Optional, if not provided we true is assumed. 
                .WithRollback(async awesomeContext =>
                {
                    await FailMiserablyToDoAnything();
                })//Func executing rollback. Optional, If not provided no rollback will be executed
                .ThenNext(async (at, ct) => await DoAnException(at))
                .WithRollback(async at => await at.AllBack())
                .ExecuteAsync(executor);
}
catch (TransactionChainException rex)
{
    Assert.Equal(ExceptionMessage, rex.InnerException.Message); //Original Exception that stopped execution is inner exception.
    Assert.Equal(RollbackFailureMessage, rex.RevertException.Message); //If rollback also failed then RevertException holds the exception.
    Assert.Equal(1, rex.CommandsRollbacked); //Notice that two (2) commands for rollback have been added, but in the example only 1 is supposed to be completed.
}
```
### Sample of Usage: TransactionChain

For complex scenarios Fluid API might not be sufficient. For such cases ICommand and ITypedCommand<T> interfaces can be implemented. This approach results in slightly larger code but at the same time gives more control over the execution.

- **ITypedCommand\<T\> Implementation**

```
private class Increment : ITypedCommand<int>
{
    private readonly int _amount;
    private readonly int _maxAmount;

    public Increment(int amount, int maxAmount)
    {
         Context = _amount = amount;
          _maxAmount = maxAmount;
    }

    public int Context { get; private set; }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
         if (cancellationToken.IsCancellationRequested)
             return Task.CompletedTask;

         Context += _amount;

         return Task.CompletedTask;
    }

     public Task<ICommand?> NextAsync()
     {
         return Context >= _maxAmount ?
              Task.FromResult(default(ICommand?)) 
              : 
              Task.FromResult<ICommand?>(new Increment(Context,
                     _maxAmount));
     }

     public Task RollbackAsync()
     {
         Context -= _amount;
         return Task.CompletedTask;
     }
}
```

-   **Executor Usage**

```
Executor executor = Helper.GetExecutor();

await executor.ExecuteAsync(new Increment(1, 10));
await executor.ExecuteAsync(new Increment(1, 2));

ar result = await executor.ExecuteAsync(new Increment(1, 10));
Assert.Equal(16, result);
            
try
{
    await executor.RollbackAsync(new System.Exception("Something exceptional happened")); // rolls back all operations performed above
}
catch (TransactionChainException tcex)
{
    Assert.Equal(9, tcex.CommandsRollbacked);
}
```

**Note**: In the code above the executor performs multiple typed commands before it gets the final result from the last call to the command. This approach is recommended for using ITypedCommand\<T\>. This way you can take intermediate results. But when an exception occurs you can call the RollbackAsync method on the executor and it will rollback all commands that were performed with this executor from the beginning. The latter is helpful in cases of time-outs since the last running command (that failed) will be tried to rollback.









