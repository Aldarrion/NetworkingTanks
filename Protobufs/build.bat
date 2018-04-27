@echo off

FOR %%f in (protocs\*.cs) DO (
    del %%f
)

FOR %%p in (proto\*.proto) DO (
  protoc --csharp_out=protocs --proto_path=proto %%p
  echo %%p compiled!
)
