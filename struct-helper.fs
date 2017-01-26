\    Copyright (C) 2016  Philip King. Smith

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
\
\   Some words that make using structures in gforth simpler
\   Works with struct and end-struct structures

: allocate-structure ( usize ustruct "allocated-structure-name" -- )  \ creates a word called allocated-structure-name
  \ usize is the quantity of the indexed array to create
  \ ustruct is the size of the structure that is returned from the name used with end-struct
	%size dup rot swap * allocate throw create , ,
	DOES> ( uindex -- uaddr ) \
		dup >r cell+ @ * r> @ + ;

\\\ comment out this line to see the following example run in gforth 0.7.9 and higher
\ The following is an example of the above word used
struct
  cell% field item
  char% field letter
  cell% field item2
end-struct stuff%

30 stuff% allocate-structure mystructure

: fillit ( -- )
  30 0 do
    777 i mystructure item2 !
    932 i * i mystructure item !
    'a' i + i mystructure letter c!
  loop ;

: seeit ( -- )
  cr
  30 0 do
    ." index:" i . space
    i mystructure item @ . space
    i mystructure letter c@ . space
    i mystructure item2 @ . cr
  loop ;

fillit seeit
