import * as ts from 'typescript';

declare const puertsRequire: any;
declare const require: any;
const CS = puertsRequire('csharp')
const fs = require('fs')
const path = require('path')

export function getCompilerOptions(): ts.CompilerOptions {
    const TSProjPath: string = getTsProjectPath();
    const compilerOptions: ts.CompilerOptions = JSON.parse(fs.readFileSync(`${TSProjPath}/tsconfig.json`)).compilerOptions;
    if (!compilerOptions) {
        return {};
    }
    compilerOptions.outDir = path.resolve(TSProjPath, compilerOptions.outDir)
    if (compilerOptions.typeRoots) {
        compilerOptions.typeRoots = compilerOptions.typeRoots.map((item: any)=> {
            return path.resolve(TSProjPath, item)
        })
    }

    return compilerOptions;
}

export function getTsProjectPath(): string {
    return path.resolve(CS.UnityEngine.Application.dataPath, '../TsProj/');
}