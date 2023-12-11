# Opswat.Challenge
Challenge to check a file against the OPSWAT's MetaDefender API.

## Prerequisites to build this solution
- Install .NET6 SDK and .NET6 Desktop Runtime from here: [https://dotnet.microsoft.com/en-us/download/dotnet/6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).
- Open `.\src\Opswat.Challenge\Opswat.Challenge.sln`.
- Restore and build the solution. This will help to create `.\src\Opswat.Challenge\Opswat.Challenge\api.key` file.
- Register account at [MetaDefender OPSWAT](https://metadefender.opswat.com/) and save the api key in the file `api.key`.
- Execute `.publish.bat` to build and publish application. The executable will, then, be found in the repo folder [**out**](out) under the name of `scanner.exe`.
- Have fun using the program `scanner.exe`.

## How to use
- Valid arguments are:

| Argument  | Description |
| --------- |-------------|
| `-File`   | Path of the file that will be used check in the program. If no argument are given by default it will be considered `-File`. |
| `-ApiKey` | Api key that will be used by server to identifiy the user. By default application can be build to include an api key **. |
| `-Hash`   | Instead of the file, the user can intergorate the server by the file's hash instead of it's path. |

\** Api key, by default, can be saved in the solution in the path: `.\src\Opswat.Challenge\Opswat.Challenge\api.key`. In order to be included as a resource in the program the file must exist and contain the value of the api key ***.

\*** Api key can be obtained by registering an account at [MetaDefender OPSWAT](https://metadefender.opswat.com/).

## Examples
```
scanner.exe D:\SomeExecutable.exe
scanner.exe -File D:\SomeExecutable.exe
scanner.exe -File D:\SomeExecutable.exe -ApiKey 01ABCDEF01234567890
scanner.exe -Hash 275a021bbfb6489e54d471899f7db9d1663fc695ec2fe2a2c4538aabf651fd0f
scanner.exe -Hash 275a021bbfb6489e54d471899f7db9d1663fc695ec2fe2a2c4538aabf651fd0f -ApiKey 01ABCDEF01234567890
```

## Documentation
- API documentation at [OPSWAT Documentation](https://docs.opswat.com/mdcloud/metadefender-cloud-api-v4).