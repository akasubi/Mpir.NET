<?php

require_once "parse.inc";

/************************************************************************
Generation of C# interface for MPIR
************************************************************************/
function CSharpMPIR_Generate($definitions)
{
    $xcore = "";
    
    //
    // output file beginning
    //
    $xcore = <<<EOS
/*
Copyright 2010 Sergey Bochkanov.

The X-MPIR is free software; you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation; either version 3 of the License, or (at your
option) any later version.

The X-MPIR is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
License for more details.

You should have received a copy of the GNU Lesser General Public License
along with the X-MPIR; see the file COPYING.LIB.  If not, write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
MA 02110-1301, USA.
*/

/*
Modifications by John Reynolds, to provide disposal of unmanaged resources,
binary import/export functions etc.
*/    

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Mpir.NET 
{

using mpz_intptr = IntPtr;
using mpq_intptr = IntPtr;
using mpf_intptr = IntPtr;
using mpfr_intptr = IntPtr;
using gmp_randstate_intptr = IntPtr;


public static partial class mpir
{
    //
    // xMPIR library handle
    private static IntPtr hxmpir = initialize_hxmpir();
    
EOS;

    //
    // generate interface
    //
    $s_ptrs = "";
    $s_delegate_defs = "";
    $s_delegates = "";
    $s_functions = "";
    foreach($definitions as $definition)
    {
        //
        // silently change 'base' parameter name to '_base' because 'base' is a C# keyword
        //
        foreach($definition['params'] as $p_key => $p_val)
            if( $p_val['name']=='base' )
                $definition['params'][$p_key]['name'] = '_base';
        
        //
        // process
        //
        if( $definition['entitytype']=='mp_constructor' )
        {
            //
            // constructor is just a function which return multiple precision type
            //
            
            // Changed by JR: use the <type>_intptr type.
            $definition['type'] = "$definition[type]_intptr";
            // $definition['type'] = "$definition[type]_handle";  -- original line
            $definition['entitytype']='function';
            
        }
        if( $definition['entitytype']=='mp_destructor' )
        {
            //
            // destructor is just a 'void' function which accepts multiple precision type
            //
            $definition['params'] = array_merge(array(array('passingtype' => 'in', 'type' => "$definition[type]_handle", 'name' => 'v')), $definition['params']);
            $definition['type'] = 'void';
            $definition['entitytype']='function';
        }
        if( $definition['entitytype']=='function' )
        {
            $s_ptrs .= "    private static IntPtr __ptr__xmpir_$definition[name] = GetProcAddressSafe(hxmpir, \"xmpir_$definition[name]\");\n";
            $s_delegate_defs .= "    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]\n";
            $s_delegate_defs .= "    ".CSharpMPIR_GenerateDelegate($definition['name'], $definition['type'], $definition['params']).";\n";
            $s_delegates .= "    private static __xmpir_$definition[name] xmpir_$definition[name] = (__xmpir_$definition[name])Marshal.GetDelegateForFunctionPointer(__ptr__xmpir_$definition[name], typeof(__xmpir_$definition[name]));\n";
            $s_functions .= CSharpMPIR_GenerateFunction($definition['name'], $definition['type'], $definition['params']);
            continue;
        }
    }
    $xcore .= <<<EOS


    //
    // Automatically generated code: pointers to functions
    //
$s_ptrs


    //
    // Automatically generated code: definitions
    //
$s_delegate_defs


    //
    // Automatically generated code: delegates
    //
$s_delegates
    
    
    //
    // Automatically generated code: functions
    //
$s_functions

EOS;
    
	/*$xcore .= <<<EOS
    [STAThread]
    static void Main(string[] args)
    {
        Console.WriteLine("123");
        return;
    }

EOS;*/
    $xcore .= "}\n}\n";
    return $xcore;
}


/************************************************************************
C# delegate declaration

$name       function name
$type       return type (may be 'void')
$params     array containing function parameters

NOTE: when $type!='void' this function generates code for a function with
'void' return type whose first argument is a pointer to $type.
************************************************************************/
function CSharpMPIR_GenerateDelegate($name, $type, $params)
{
    $arr = array();
    if( $type!='void' )
        $params = array_merge(array(array('passingtype' => 'out', 'type' => $type, 'name' => 'result')), $params);
    foreach($params as $p)
    {
        $s = CSharpMPIR_CSharpRawType($p['type']);
        if( $p['passingtype']=='out' )
            $s = "out $s";
        if( ($p['passingtype']=='inout') || ($p['passingtype']=='ref'))
            $s = "ref $s";
        if( $p['passingtype']!='in' and $p['passingtype']!='out' and $p['type']=='str_pointer' )
            exit("\nString pointers may be passed as 'in' or 'out' parameters only! 'ref' and 'inout' are not allowed!\n");
        $arr[] = "$s $p[name]";        
    }
    $params = implode(", ",$arr);
    return "private delegate int __xmpir_$name($params)";    
}


/************************************************************************
C# function declaration and body

$name       function name
$type       return type (may be 'void')
$params     array containing function parameters
************************************************************************/
function CSharpMPIR_GenerateFunction($name, $type, $params)
{
    $result = "";
    $pre_call_code = "";
    $post_call_code = "";
    $arr_decl = array();
    $arr_call = array();
    if( $type!='void' )
    {
        $params = array_merge(array(array('passingtype' => 'out', 'type' => $type, 'name' => 'result')), $params);
        $pre_call_code .= "        ".CSharpMPIR_CSharpType($type)." result;\n";
    }
    foreach($params as $p)
    {
        //
        // parameters declaration (skip 'result' parameter)
        // 
        if( $p['name']!='result' )
        {
            $s = CSharpMPIR_CSharpType($p['type']);
            if( $p['passingtype']=='out' )
                $s = "out $s";
            if( ($p['passingtype']=='inout') || ($p['passingtype']=='ref'))
                $s = "ref $s";
            if( $p['passingtype']!='in' and $p['passingtype']!='out' and $p['type']=='str_pointer' )
                exit("\nString pointers may be passed as 'in' or 'out' parameters only! 'ref' and 'inout' are not allowed!\n");
            $arr_decl[] = "$s $p[name]";
        }
        
        //
        // prepare function call
        //
        $s = "";
        switch($p['type'])
        {
        case 'uint32':
        case 'sint32':
        case 'double':
        case 'mp_bitcnt_t':
        case 'mp_exp_t':
            $s = "$p[name]";
            break;
        case 'str_pointer':
            $s = "__$p[name]";
            break;
        case 'mpz_handle':
        case 'mpq_handle':
        case 'mpf_handle':
        case 'mpfr_handle':
        case 'gmp_randstate_handle':
            $s = "$p[name].val";
            break;

        // Added by JR: intptr.
        case 'mpz_intptr':
        case 'mpq_intptr':
        case 'mpf_intptr':
        case 'mpfr_intptr':
        case 'gmp_randstate_intptr':
            $s = "$p[name]";
            break;

        default:
            exit("\nCSharpMPIR_GenerateFunction(): unknown type '$p[type]'\n");
        }
        if( $p['passingtype']=='out' )
            $s = "out $s";
        if( ($p['passingtype']=='inout') || ($p['passingtype']=='ref'))
            $s = "ref $s";
        $arr_call[] = $s;
        
        //
        // Handle input strings:
        // 1. copy managed strings to unmanaged memory allocated by lib
        // 2. free unmanaged memory (after call)
        //
        if( $p['passingtype']=='in' and $p['type']=='str_pointer' )
        {
            $pre_call_code .= "        byte[] __ba_$p[name] = System.Text.Encoding.UTF8.GetBytes($p[name]+\"\\0\");\n";
            $pre_call_code .= "        IntPtr __$p[name];\n";
            $pre_call_code .= "        __retval = xmpir_malloc(out __$p[name], $p[name].Length+1);\n";
            $pre_call_code .= "        if( __retval!=0 ) HandleError(__retval);\n";
            $pre_call_code .= "        Marshal.Copy(__ba_$p[name], 0, __$p[name], $p[name].Length+1);\n";
            $post_call_code .= "       __retval = xmpir_free(__$p[name]);\n";
            $post_call_code .= "       if( __retval!=0 ) HandleError(__retval);\n";
        }        
        
        //
        // Handle output strings:
        // 1. declare variable
        // 2. copy to managed memory (after call)
        // 3. free unmanaged memory (after call)
        //
        if( $p['passingtype']=='out' and $p['type']=='str_pointer' )
        {
            $pre_call_code .= "        IntPtr __$p[name];\n";            
            $post_call_code .= "       $p[name] = Marshal.PtrToStringAnsi(__$p[name]);\n";
            $post_call_code .= "       __retval = xmpir_free(__$p[name]);\n";
            $post_call_code .= "       if( __retval!=0 ) HandleError(__retval);\n";
        }
    }
    $decl_params = implode(", ",$arr_decl);
    $call_params   = implode(", ",$arr_call);
    $rettype = CSharpMPIR_CSharpType($type);
    if( $type!='void' )
        $post_call_code .= "        return result;\n";
    $result .= <<<EOS
    public static $rettype $name($decl_params)
    {
        int __retval;
$pre_call_code        __retval= xmpir_$name($call_params);
        if( __retval!=0 ) HandleError(__retval);
$post_call_code    }

EOS;
  
    return $result;
}


/************************************************************************
converts X-type to C# type from delegate declaration
************************************************************************/
function CSharpMPIR_CSharpRawType($type)
{
    switch($type)
    {
        case 'uint32':
            return "uint";
        case 'sint32':
            return "int";
        case 'double':
            return "double";
        case 'mp_bitcnt_t':
            return "ulong";
        case 'mp_exp_t':
            return "long";
        case 'str_pointer':
        case 'mpz_handle':
        case 'mpq_handle':
        case 'mpf_handle':
        case 'mpfr_handle':
        case 'gmp_randstate_handle':
            return "IntPtr";

        // intptr added by JR.
        case 'mpz_intptr':
        case 'mpq_intptr':
        case 'mpf_intptr':
        case 'mpfr_intptr':
        case 'gmp_randstate_intptr':
            return "IntPtr";
    }
    exit("\nCSharpMPIR_CSharpRawType(): no such type - '$type'\n");
}

/************************************************************************
converts X-type to C# type from function declaration
************************************************************************/
function CSharpMPIR_CSharpType($type)
{
    switch($type)
    {
        case 'void':
            return "void";
        case 'uint32':
            return "uint";
        case 'sint32':
            return "int";
        case 'double':
            return "double";
        case 'str_pointer':
            return "string";
        case 'mp_bitcnt_t':
            return "ulong";
        case 'mp_exp_t':
            return "long";
        case 'mpz_handle':
            return "mpz_t";
        case 'mpq_handle':
            return "mpq_t";
        case 'mpf_handle':
            return "mpf_t";
        case 'mpfr_handle':
            return "mpfr_t";
        case 'gmp_randstate_handle':
            return "gmp_randstate_t";

        // intptr added by JR.
        case 'mpz_intptr':
        case 'mpq_intptr':
        case 'mpf_intptr':
        case 'mpfr_intptr':
        case 'gmp_randstate_intptr':
            return $type;
    }
    exit("\nCSharpMPIR_CSharpType(): no such type - '$type'\n");
}

?>