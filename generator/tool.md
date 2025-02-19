# Running Kiota from the dotnet tool

1. Navigate to [New personal access token](https://github.com/settings/tokens/new) and generate a new token. (permissions: read:package).
1. Copy the token, you will need it later.
1. Enable the SSO on the token if you are a Microsoft employee.
1. Create a `nuget.config` file in the current directory with the following content.

    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
        <packageSources>
            <add key="GitHub" value="https://nuget.pkg.github.com/microsoft/index.json" />
        </packageSources>
        <packageSourceCredentials>
            <GitHub>
                <add key="Username" value="" /><!-- your github username -->
                <!-- your github PAT: read:pacakges with SSO enabled for the Microsoft org (for microsoft employees only) -->
                <add key="ClearTextPassword" value="" />
            </GitHub>
        </packageSourceCredentials>
    </configuration>
    ```

1. Execute the following command to install the tool.

    ```Shell
    dotnet tool install --global --configfile nuget.config kiota
    ```

1. Execute the following command to run kiota.

    ```Shell
    kiota -d /some/input/description.yml -o /some/output/path --language csharp -n samespaceprefix
    ```
