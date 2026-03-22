; Shipped analyzer releases
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0.0

### New Rules

 Rule ID  | Category     | Severity | Notes                                                 
----------|--------------|----------|-------------------------------------------------------
 NINVK001 | NativeInvoke | Error    | Type must be partial to use [NativeImport]            
 NINVK002 | NativeInvoke | Error    | Property must be static partial to use [NativeImport] 
 NINVK003 | NativeInvoke | Error    | Property type must be interface to use [NativeImport] 
 NINVK004 | NativeInvoke | Error    | Method has non-blittable signature                    

## Release 1.3.5

### New Rules

 Rule ID  | Category     | Severity | Notes                                            
----------|--------------|----------|--------------------------------------------------
 NINVK005 | NativeInvoke | Warning  | Interface has no valid methods to generate       
 NINVK006 | NativeInvoke | Error    | Missing library name in [NativeImport] attribute 

## Release 1.3.6

### New Rules

 Rule ID  | Category     | Severity | Notes                                 
----------|--------------|----------|---------------------------------------
 NINVK007 | NativeInvoke | Warning  | Invalid attribute argument type/value 
