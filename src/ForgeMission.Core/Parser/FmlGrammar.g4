grammar FmlGrammar;

// Parser rules

program
    : declaration* EOF
    ;

declaration
    : mission
    | expert
    ;

mission
    : MISSION UPPER_ID EQUALS pipeline
    ;

expert
    : EXPERT UPPER_ID EQUALS pipeline
    ;

pipeline
    : step (PIPE step)*
    ;

step
    : UPPER_ID
    ;

// Lexer rules

MISSION : 'mission' ;
EXPERT  : 'expert'  ;
PIPE    : '|>'      ;
EQUALS  : '='       ;

UPPER_ID
    : [A-Z][a-zA-Z0-9]*
    ;

LOWER_ID
    : [a-z][a-zA-Z0-9]*
    ;

WS
    : [ \t\r\n]+ -> skip
    ;
