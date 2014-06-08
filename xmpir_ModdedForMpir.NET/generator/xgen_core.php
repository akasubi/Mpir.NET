<?php

error_reporting (E_ALL & ~E_NOTICE);
require_once "parse.inc";
require_once "xmpir.inc";
require_once "xmpir4cs.inc";

$p = XGenParse("mpir.x");
echo filter_rn(XMPIR_GenerateWinCore($p));

function filter_rn($s)
{
    $s = str_replace("\r", "", $s);
    $s = str_replace("\n", "\r\n", $s);
    return $s;
}

?>