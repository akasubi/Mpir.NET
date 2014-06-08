<?php

require_once "parse.inc";

/************************************************************************
Generation of xMPIR core for 32/64-bit Windows.
************************************************************************/
function XMPIR_GenerateWinCore($definitions)
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
Minor modifications by John Reynolds, to provide binary import/export
functions. Use e.g. Kdiff to see exact changes from orginal X-MPIR.
*/    


#include <stdio.h>
#include <stdlib.h>
#include <mpir.h>

#ifdef XMPIR_FOR_WINDOWS
#define DLLEXPORT __declspec(dllexport)
#endif
#ifdef XMPIR_FOR_LINUX
#define DLLEXPORT
#endif
enum
{
    XMPIR_OK = 0,
    XMPIR_MALLOC_ERROR = 1,
    XMPIR_DIV_BY_ZERO_ERROR = 2,
    XMPIR_32_64_ERROR = 3
};
typedef struct { mpz_t val; } mpz_wrapper;
typedef struct { mpq_t val; } mpq_wrapper;
typedef struct { mpf_t val; } mpf_wrapper;
typedef struct { gmp_randstate_t val; } gmp_randstate_wrapper;

DLLEXPORT int xmpir_malloc(void **p, int size)
{
    *p = malloc(size);
    return *p ? XMPIR_OK : XMPIR_MALLOC_ERROR;
}
DLLEXPORT int xmpir_free(void *p)
{
    free(p);
    return XMPIR_OK;
}


DLLEXPORT void Mpir_internal_mpz_import(mpz_t rop, size_t count, int order, size_t size, int endian, size_t nails, const void *op)
{
    mpz_import(rop, count, order, size, endian, nails, op);
}
DLLEXPORT void *Mpir_internal_mpz_export(void *rop, size_t *countp, int order, size_t size, int endian, size_t nails, mpz_t op)
{
    return mpz_export(rop, countp, order, size, endian, nails, op);
}


char * mpz_get_string(int base, mpz_srcptr op)
{
    return mpz_get_str(NULL, base, op);
}
char * mpq_get_string(int base, mpq_srcptr op)
{
    return mpq_get_str(NULL, base, op);
}
char * mpf_get_string(mp_exp_t *expptr, int base, size_t n_digits,  mpf_srcptr op)
{
    return mpf_get_str(NULL, expptr, base, n_digits, op);
}
void xmpir_dummy()
{
}
int xmpir_dummy_add(int a, int b)
{
    return a+b;
}
int xmpir_dummy_3mpz(mpz_srcptr op0, mpz_srcptr op1, mpz_srcptr op2)
{
    return 0;
}


EOS;

    //
    // generate interface
    //
    foreach($definitions as $definition)
    {
        if( $definition['entitytype']=='mp_constructor' )
        {
            $decl = XMPIR_GenerateCExports($definition['name'], "$definition[type]_handle", $definition['params']);
            $xcore .= "$decl\n";
            $xcore .= "{\n";
            $tmp = XMPIR_GenerateCall($definition['params']);
            $precall = "$tmp[precall]    *result = ($definition[type]_wrapper*)malloc(sizeof($definition[type]_wrapper));\n";
            $call = "    $definition[name]((*result)->val";
            if( count($tmp['params'])!=0 )
                $call .= ", ".implode(", ", $tmp['params']);
            $call .= ");\n";
            $postcall = $tmp['postcall'];            
            $xcore .= "{$precall}{$call}{$postcall}";
            $xcore .= "    return XMPIR_OK;\n";
            $xcore .= "}\n";
            continue;
        }
        if( $definition['entitytype']=='mp_destructor' )
        {
            $decl = XMPIR_GenerateCExports(
                $definition['name'], 
                "void", 
                array_merge(array(array('passingtype' => 'in', 'type' => "$definition[type]_handle", 'name' => '_v')), $definition['params']));
            $xcore .= "$decl\n";
            $xcore .= "{\n";
            $tmp = XMPIR_GenerateCall($definition['params']);
            $precall = $tmp['precall'];
            $call = "    $definition[name](_v->val";
            if( count($tmp['params'])!=0 )
                $call .= ", ".implode(", ", $tmp['params']);
            $call .= ");\n";
            $postcall = $tmp['postcall'];            
            $xcore .= "{$precall}{$call}{$postcall}";
            
            $xcore .= "    free(_v);\n";
            $xcore .= "    return XMPIR_OK;\n";
            $xcore .= "}\n";
            continue;
        }
        if( $definition['entitytype']=='function' )
        {
            $decl = XMPIR_GenerateCExports($definition['name'], $definition['type'], $definition['params']);
            $xcore .= "$decl\n";
            $xcore .= "{\n";
            $tmp = XMPIR_GenerateCall($definition['params']);
            $precall = $tmp['precall'];
            if( $definition['type']=='void' )
                $call = "    $definition[name](".implode(", ", $tmp['params']).");\n";
            elseif( $definition['type']=='mp_bitcnt_t' || $definition['type']=='mp_exp_t')
                $call = "    *__result = $definition[name](".implode(", ", $tmp['params']).");\n";
            else
                $call = "    *result = $definition[name](".implode(", ", $tmp['params']).");\n";
            $postcall = $tmp['postcall'];            
            $xcore .= "{$precall}{$call}{$postcall}";
            $xcore .= "    return XMPIR_OK;\n";
            $xcore .= "}\n";
            continue;
        }
    }   
    
    return $xcore;
}


/************************************************************************
C function declaration for XMPIR core (just name, pragmas and parameters)

$name       function name
$type       return type (may be 'void')
$params     array containing function parameters

NOTE: when $type!='void' this function generates code for a function with
'void' return type whose first argument is a pointer to $type.

NOTE2: parameters which require complex marshalling have their names
prepended by double underscores.
************************************************************************/
function XMPIR_GenerateCExports($name, $type, $params)
{
    $arr = array();
    if( $type!='void' )
        $params = array_merge(array(array('passingtype' => 'out', 'type' => $type, 'name' => 'result')), $params);
    foreach($params as $p)
    {
        $s = XMPIR_CoreType($p['type']);
        if( $p['passingtype']!='in' )
            $s .= "*";
        if( $p['passingtype']!='in' and $p['passingtype']!='out' and $p['type']=='str_pointer' )
            exit("\nString pointers may be passed as 'in' or 'out' parameters only! 'ref' and 'inout' are not allowed!\n");
        if( $p['type']=='mp_bitcnt_t' || $p['type']=='mp_exp_t')
            $p['name'] = "__$p[name]";
        $arr[] = "$s $p[name]";        
    }
    $params = implode(", ",$arr);
    return "DLLEXPORT int xmpir_$name($params)";    
}


/************************************************************************
Function call, preparation and post-call handling of arguments.

This functions returns associative array:
* $result['precall']  contains source code which prepares function call.
* $result['postcall'] contains source code which handles results
* $result['params'] is array containing source code for actual parameters
  example: ('a', 'b', '&c', 'd->val', '__f')

This function makes several assumtions about transformation of parameter'
names. See NOTE 2 from XMPIR_GenerateCExports() for more info.
************************************************************************/
function XMPIR_GenerateCall($params)
{
    $result = array();
    $result['precall'] = "";
    $result['params'] = array();
    $result['postcall'] = "";
    foreach($params as $p)
    {
        //
        // different types are handled differently
        //
        $s = "";
        if( XGenIsMPType($p['type']) )
        {
            // multiple precision type
            if( $p['passingtype']=='in' )
                $s = "$p[name]->val";
            else
                exit("\nXMPIR_GenerateWinCore(): multiple precision type can't be output parameter!\n");
        }
        elseif( $p['type']=='mp_bitcnt_t' || $p['type']=='mp_exp_t')
        {
            // mp_bitcnt_t or mp_exp_t - use proxy variable to catch situations when input doesn't fit into mp_bitcnt_t/mp_exp_t
            if( $p['passingtype']=='in' )
            {
                $result['precall'] = "    volatile $p[type] $p[name];\n$result[precall]    $p[name] = __$p[name];\n    if( $p[name]!=__$p[name] ) return XMPIR_32_64_ERROR;\n";
                $s = "$p[name]";
            }
            if( $p['passingtype']=='out' )
            {
                $result['precall'] = "    $p[type] $p[name];\n$result[precall]";
                $s = "&$p[name]";
                $result['postcall'] = "$result[postcall]    *__$p[name] = $p[name];\n";
            }
            if( ($p['passingtype']=='ref') || ($p['passingtype']=='inout') )
            {
                $result['precall'] = "    volatile $p[type] $p[name];\n$result[precall]    $p[name] = __$p[name];\n    if( $p[name]!=__$p[name] ) return XMPIR_32_64_ERROR;\n";
                $s = "&$p[name]";
                $result['postcall'] = "$result[postcall]    *__$p[name] = $p[name];\n";
            }
        }
        else
            $s = "$p[name]";
        $result['params'][] = $s;
    }
    return $result;
}
            
/************************************************************************
converts X-type to MPIR core type
************************************************************************/
function XMPIR_CoreType($type)
{
    switch($type)
    {
        case 'uint32':
            return "unsigned int";
        case 'sint32':
            return "signed int";
        case 'double':
            return "double";
        case 'str_pointer':
            return "char*";
        case 'mp_bitcnt_t':
            return "unsigned long long";
        case 'mp_exp_t':
            return "signed long long";
        case 'mpz_handle':
            return "mpz_wrapper*";
        case 'mpq_handle':
            return "mpq_wrapper*";
        case 'mpf_handle':
            return "mpf_wrapper*";
        case 'mpfr_handle':
            return "mpfr_wrapper*";
        case 'gmp_randstate_handle':
            return "gmp_randstate_wrapper*";
    }
    exit("\nXMPIR_CoreType: No such type: '$type'\n");
}

?>