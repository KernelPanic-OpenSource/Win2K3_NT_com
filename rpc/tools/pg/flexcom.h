// Copyright (c) 1993-1999 Microsoft Corporation

/* common macro definitions for C/FTL programs generated by flex */


/* returned upon end-of-file */
#define YY_END_TOK 0

/* action number for an "end-of-file was seen and yywrap indicated that we
 * should continue processing"
 */
#define YY_NEW_FILE -1

/* action number for "the default action should be done" */
#define YY_DO_DEFAULT -2

#ifndef BUFSIZ
#include <stdio.h>
#endif

#define YY_BUF_SIZE (BUFSIZ * 2) /* size of input buffer */

/* number of characters one rule can match.  One less than YY_BUF_SIZE to make
 * sure we never access beyond the end of an array
 */
#define YY_BUF_MAX (YY_BUF_SIZE - 1)

/* we will never use more than the first YY_BUF_LIM + YY_MAX_LINE positions
 * of the input buffer
 */
#ifndef YY_MAX_LINE
#define YY_MAX_LINE BUFSIZ
#endif

#define YY_BUF_LIM (YY_BUF_MAX - YY_MAX_LINE)

/* copy whatever the last rule matched to the standard output */

#define ECHO fputs( yytext, yyout )

/* gets input and stuffs it into "buf".  number of characters read, or YY_NULL,
 * is returned in "result".
 */
#if 1
#define YY_INPUT(buf,result,max_size) \
	{	\
	result = fread( buf, 1, max_size, yyin );	\
	if( ferror( yyin ) )	\
	    YY_FATAL_ERROR( "fread() in flex scanner failed" );	\
	}
#else // 0
#define YY_INPUT(buf,result,max_size) \
	if ( (result = read( fileno(yyin), buf, max_size )) < 0 ) \
	    YY_FATAL_ERROR( "read() in flex scanner failed" );
#endif // 0

#define YY_NULL 0

/* macro used to output a character */
#define YY_OUTPUT(c) putc( c, yyout );

/* report a fatal error */
#define YY_FATAL_ERROR(msg) \
	{ \
	fputs( msg, stderr ); \
	putc( '\n', stderr ); \
	exit( 1 ); \
	}

/* returns the first character of the matched text */
#define YY_FIRST_CHAR yy_ch_buf[yy_b_buf_p]

/* default yywrap function - always treat EOF as an EOF */
#define yywrap() 1

/* enter a start condition.  This macro really ought to take a parameter,
 * but we do it the disgusting crufty way that old Unix-lex does it
 */
#define BEGIN yy_start = 1 +

/* callable from YY_INPUT to set things up so that '%' will match.  Proper
 * usage is "YY_SET_BOL(array,pos)"
 */
#define YY_SET_BOL(array,pos) array[pos - 1] = '\n';

/* default declaration of generated scanner - a define so the user can
 * easily add parameters
 */
#define YY_DECL int yylex()

/* return all but the first 'n' matched characters back to the input stream */
#define yyless(n) \
	{ \
	YY_DO_BEFORE_SCAN; /* undo effects of setting up yytext */ \
	yy_c_buf_p = yy_b_buf_p + n - 1; \
	YY_DO_BEFORE_ACTION; /* set up yytext again */ \
	}

/* code executed at the end of each rule */
#define YY_BREAK break;
