\ This Gforth code is a sqlite3 wrapper library 
\    Copyright (C) 2013  Philip King. Smith

\    This program is free software: you can redistribute it and/or modify
\    it under the terms of the GNU General Public License as published by
\    the Free Software Foundation, either version 3 of the License, or
\    (at your option) any later version.

\    This program is distributed in the hope that it will be useful,
\    but WITHOUT ANY WARRANTY; without even the implied warranty of
\    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\    GNU General Public License for more details.

\    You should have received a copy of the GNU General Public License
\    along with this program.  If not, see <http://www.gnu.org/licenses/>.

\ Note the following is needed on your Debian (tested with wheezy):
\ sudo apt-get install sqlite3 libsqlite3-dev
\ These apt-get installs are needed to connect this code to sqlite3 code

\ I will put here how to use this code when i get there!

include string.fs

clear-libs
c-library mysqlite3

s" sqlite3" add-lib

\c #include <stdio.h>
\c #include "sqlite3.h"
\c #include <string.h>
\c
\c char * theBuffer = 0;
\c int maxBuffsize = 0;
\c char * separator = 0;
\c int bufferok =0;
\c
\c static int callback( void * NotUsed, int argc, char ** argv, char ** azColName ) {
\c int i;
\c char temp[maxBuffsize] ;
\c int tempSize;
\c int tempBuffsize;
\c
\c for(i = 0; i < argc ; i++) {
\c      sprintf(temp,"%s",argv[i] ? argv[i] : "NULL");
\c      strcat(temp,separator);    
\c      tempSize = strlen(temp);
\c      tempBuffsize = strlen(theBuffer);
\c      if((tempBuffsize + tempSize + 1) < maxBuffsize){
\c           strcat(theBuffer,temp);
\c           } else {
\c           bufferok = 1;
\c           return 0; }
\c      }
\c if((strlen(theBuffer) + strlen(separator) + 1) < maxBuffsize) {
\c    strcat(theBuffer,separator);
\c    } else {
\c    bufferok = 1; }
\c return 0;
\ This was from testing but i left it here to remember the variable meanings!
\ \c for(i=0; i < argc ; i++) {
\ \c    printf( "%s = %s\n", azColName[i], argv[i] ? argv[i] : "NULL" );
\ \c    }
\c }
\  filename is a string for the filename of the database.
\  sqlite3_cmds is a string of the comands to send to sqlite3.
\  sqlite3_ermsg is the returned string of errors if any happen.
\  buffer is a string that will contain the result of the sqlite3 commands if any results are expected!
\  buffsize is the size or the buffer string including room for the null terminator
\  sep is a string that contains the field and record seperator to incert into the returned result in buffer.
\  buffok is a char string of dimention one that contains 0 or 1 depending on if the result returned fit in the buffsize of buffer.
\  Note all these strings are need to be zero terminated at entry to this function.
\c 
\c int sqlite3to4th( const char * filename, char * sqlite3_cmds, char * sqlite3_ermsg , char * buffer, int buffsize , char * sep, char * buffok) {
\c sqlite3 *db;
\c char * zErrMsg = 0 ;
\c int rc = 0 ;
\c theBuffer = buffer;
\c maxBuffsize = buffsize;
\c separator = sep;
\c buffok[0]=0;
\c
\c rc = sqlite3_open( filename, &db ) ;
\c if( rc ) {
\c    sprintf( sqlite3_ermsg,"Can't open database: %s\n", sqlite3_errmsg( db ) );
\c    sqlite3_close( db );
\c    return 1;
\c }
\c    
\c rc = sqlite3_exec( db, sqlite3_cmds, callback, 0, &zErrMsg );
\c if( rc!=SQLITE_OK ) {
\c   sprintf( sqlite3_ermsg, "SQL error: %s\n", zErrMsg );
\c   sqlite3_free( zErrMsg );
\c }
\c sqlite3_close( db );
\c if(bufferok==1) buffok[0]=1;
\c return 0;
\c }
\c

\ **** sqlite3 gforth wrappers ****

c-function sqlite3 sqlite3to4th a a a a n a a -- n
\ note that c strings are always null terminated unlike gforth strings! 
    
end-c-library

: z$! ( caddr u addr1 -- ) \ works with $! from string.fs but adds a null at end or string to pass to c code
    swap 1 + swap dup { pointer } $! \ note $! will free allocated memory if it needs to
    0 pointer $@ 1 - + c! ;

: z$@ ( caddr -- caddr1 ) \ returns the pointer to char ready for c code to use!
    $@ drop ;

struct
    cell% field dbname-$
    cell% field dbcmds-$
    cell% field dberrors-$
    cell% field retbuff-$
    cell% field retbuffmaxsize-cell
    cell% field seperator-$
    char% field buffok-flag
end-struct sqlite3message%

create sqlmessg
\ sqlmessg sqlite3message% %size dup allot erase 
sqlite3message%  %allot drop

: mkretbuff ( nsize -- )
    sqlmessg retbuffmaxsize-cell !
    sqlmessg retbuffmaxsize-cell @ allocate throw { addr } 
    addr sqlmessg retbuffmaxsize-cell @ erase
    addr sqlmessg retbuffmaxsize-cell @ sqlmessg retbuff-$ z$!
    addr free throw ;

: mkerrorbuff ( -- )
    80 allocate throw { addr }
    addr 80 erase
    addr 80 sqlmessg dberrors-$ z$!
    addr free throw ;

: initsqlmessg ( -- ) \ clear all sqlmessg data 
    s" " sqlmessg dbname-$ z$!
    s" " sqlmessg dbcmds-$ z$!
    mkerrorbuff
    200 mkretbuff \ start with a return string buffer of 200 
    s" ," sqlmessg seperator-$ z$!
    0 sqlmessg buffok-flag c! ;

initsqlmessg \ structure now has allocated memory 

: dbname ( caddr u -- )
    sqlmessg dbname-$ z$! ;

: dbcmds ( caddr u -- )
    sqlmessg dbcmds-$ z$! ;

: dbfieldseparator ( caddr u -- )
    sqlmessg seperator-$ z$! ;

: sendsqlite3cmd ( -- nerror ) \ will send the commands to sqlite3 and nerror contains false if no errors
    mkerrorbuff
    200 mkretbuff
    sqlmessg dbname-$ z$@
    sqlmessg dbcmds-$ z$@
    sqlmessg dberrors-$ z$@
    sqlmessg retbuff-$ z$@
    sqlmessg retbuffmaxsize-cell @
    sqlmessg seperator-$ z$@
    sqlmessg buffok-flag 
    sqlite3
    \ remember to put here the buffok-flag check to see if result buffer overflow has happened!
;