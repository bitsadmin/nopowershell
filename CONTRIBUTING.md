# Contributing 

Add your own cmdlets by submitting a pull request.
## Aim
- Maintain .NET 2.0 compatibility in order to support the broadest range of operating systems
- In case for whatever reason .NET 2.0 compatibility is not possible, add the `#if` preprocessor directive to the class specifying the unsupported .NET versions (for examples check the `*-Archive` cmdlets)

## Instructions
Use the TemplateCommand.cs file in the Commands folder to construct new cmdlets. The TemplateCommand cmdlet is hidden from the list of available cmdlets, but can be called in order to understand its workings. This command looks as follows: `Get-TemplateCommand [-MyFlag] -MyInteger [Int32] -MyString [Value]` and is also accessible via alias `gtc`.

### Example usages
| Action | Command |
| - | - |
| Simply run with default values | `gtc` |
| Run with the -MyFlag parameter which executes the 'else' statement | `gtc -MyFlag` |
| Run with the -MyInteger parameter which changes the number of iterations from its default number of 5 iterations to whatever number is provided | `gtc -MyInteger 10` |
| Run with the -MyString parameter which changes the text that is printed from its default value of 'Hello World' to whatever string is provided | `gtc -MyString "Bye PowerShell"` |
| Combination of parameters | `gtc -MyInteger 10 -MyString "Bye PowerShell"` |
| Combination of parameters - Using fact that MyString is the only mandatory parameter for this command | `gtc -MyInteger 10 "Bye PowerShell"` |
| Command in combination with a couple of data manipulators in the pipe | `gtc "Bye PowerShell" -MyInteger 30 \| ? Attribute2 -Like Line1* \| select Attribute2 \| fl` |

Execute the following steps to implement your own cmdlet:
1. Download Visual Studio Community from https://visualstudio.microsoft.com/downloads/
    * In the installer select the **.NET desktop development** component.
    * From this component no optional modules are required for developing NoPowerShell modules.
2. Make sure to have the .NET 2 framework installed: `OptionalFeatures.exe` -> '.NET Framework 3.5 (includes .NET 2.0 and 3.0)'.
3. Clone this repository and create a copy of the **TemplateCommand.cs** file.
    * In case you are implementing a native PowerShell command, place it in folder the corresponding to the _Source_ attribute when executing in PowerShell: `Get-Command My-Commandlet`.
        * Moreover, use the name of the _Source_ attribute in the command's namespace.
        * Example of a native command: `Get-Command Get-Process` -> Source: `Microsoft.PowerShell.Management` -> Place the .cs file in the **Management** subfolder and use `NoPowerShell.Commands.Management` namespace.
    * In case it is a non-native command, place it in the **Additional** folder and use the `NoPowerShell.Commands.Additional` namespace.
4. Update the `TemplateCommand` classname and its constructor name.
5. Update the static **Aliases** variable to the command and aliases you want to use to call this cmdlet. For native PowerShell commands you can lookup the aliases using `Get-Alias | ? ResolvedCommandName -EQ My-Commandlet` to obtain the list of aliases. Always make sure the full command is the first "alias", for example: `Get-Alias | ? ResolvedCommandName -EQ Get-Process` -> Aliases are: `Get-Process`, `gps`, `ps`
6. Update the static **Synopsis** variable to a small text that describes the command. This will be shown in the help.
7. Update the arguments supported by the command by adding _StringArguments_, _BoolArguments_ and _IntegerArguments_ to the static **SupportedArguments** variable.
8. In the Execute function:
    1. Fetch the values of the _StringArguments_, _BoolArguments_ and _IntegerArguments_ as shown in the examples;
    2. Based on the parameters provided by the user, perform your actions;
    3. Make sure all results are stored in the `_results` variable.
9. Remove all of the template sample code and comments from the file to keep the source tidy.