<?php

/************************************************************************
This function parses X-file specified by $filename and returns  interface
definition.
************************************************************************/
function XGenParse($filename)
{
    $result = array();
    
    //
    // interface definition file is processed line by line
    //
    $lines = file($filename, FILE_IGNORE_NEW_LINES|FILE_SKIP_EMPTY_LINES);
    foreach($lines as $line)
    {
        $entity = array();
        
        //
        // preprocess line: remove unnecessary spaces
        //
        $line = XGenStripSpaces($line);
        //echo "'$line'\n";
        
        //
        // determine what kind of entity is defined by current line
        //
        $selector = substr($line, 0, strcspn($line, " <"));
        
        //
        //  process entities
        //
        $entity['entitytype'] = $selector;
        if( $selector=='mp_constructor' )
        {
            if( preg_match("/^mp_constructor<(mpz|mpq|mpf|gmp_randstate)>(\w+)\(([^\)]*)\);$/", $line, $matches)!=1 )
                exit("\n'$line' is not correct constructor definition\n");
            $entity['type'] = $matches[1];
            $entity['name'] = $matches[2];
            $entity['params'] = XGenParseParams($matches[3]);
            $result[] = $entity;
            continue;
        }
        if( $selector=='mp_destructor' )
        {
            if( preg_match("/^mp_destructor<(mpz|mpq|mpf|gmp_randstate)>(\w+)\(([^\)]*)\);$/", $line, $matches)!=1 )
                exit("\n'$line' is not correct destructor definition\n");
            $entity['type'] = $matches[1];
            $entity['name'] = $matches[2];
            $entity['params'] = XGenParseParams($matches[3]);
            $result[] = $entity;
            continue;
        }
        if( $selector=='function' )
        {
            if( preg_match("/^function (void|str_pointer|uint32|sint32|double|mp_bitcnt_t|mp_exp_t) (\w+)\(([^\)]*)\);$/", $line, $matches)!=1 )
                exit("\n'$line' is not correct function definition\n");
            $entity['type'] = $matches[1];
            $entity['name'] = $matches[2];
            $entity['params'] = XGenParseParams($matches[3]);
            $result[] = $entity;
            continue;
        }
    }
    return $result;
}

/************************************************************************
This function parses parameters specification
(text like "in mpz_handle rop, in uint32 op").

Internal function.
************************************************************************/
function XGenParseParams($str)
{
    $str = trim($str);
    $params = array();
    if( $str=="" )
        return $params;
    $arr = explode(",", $str);
    foreach($arr as $paramstr)
    {
        $paramstr = XGenStripSpaces($paramstr);
        if( preg_match("/^(in|out|ref|inout) (uint32|sint32|double|str_pointer|mp_bitcnt_t|mp_exp_t|gmp_randstate_handle|mpz_handle|mpq_handle|mpf_handle|mpfr_handle|mpir_si|mpir_ui) (\w+)$/", $paramstr, $matches)!=1 )
            exit("\n'$paramstr' is not correct X-gen parameter\n");
        $param = array();
        $param['passingtype'] = $matches[1];
        $param['type'] = $matches[2];
        $param['name'] = $matches[3];
        $params[] = $param;        
    }
    return $params;
}

/************************************************************************
This function strips spaces from interface definition which makes parsing
less difficult.

Internal function.
************************************************************************/
function XGenStripSpaces($str)
{
    $str = trim($str);
    $str = preg_replace("/ +/", " ", $str);
    $str = preg_replace("/ ?(<|>|\(|\)|\,) ?/", "$1", $str);
    return $str;
}

/************************************************************************
returns true if $type is multiple precision type
************************************************************************/
function XGenIsMPType($type)
{
    return $type=='mpz_handle' or $type=='mpq_handle' or $type=='mpf_handle' or $type=='mpfr_handle' or $type=='gmp_randstate_handle';
}

?>