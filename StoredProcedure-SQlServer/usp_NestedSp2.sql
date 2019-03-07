USE [SANDBOX]
;

/****** Object:  StoredProcedure [dbo].[usp_NestedSP2]    Script Date: 3/7/2019 1:23:49 PM ******/
SET ANSI_NULLS ON
;

SET QUOTED_IDENTIFIER ON
;

CREATE PROCEDURE
[dbo].[usp_NestedSP2]
@i_TraceOn		TINYINT		= 0
AS
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON
	SET ANSI_NULLS ON
	SET QUOTED_IDENTIFIER ON

	-- Parameters.
	DECLARE	
	@time				VARCHAR(14)

	-- Pre Processing.
	DECLARE
	@PROC_NAME			VARCHAR(256),
	@TIME_NOW			DATETIME,
	-- File handlings.
	@OutputFile			NVARCHAR(100),    
	@FilePath			NVARCHAR(100),    
	@bcpCommand			NVARCHAR(1000),
	@command			VARCHAR(1000)

	-- Set constants.
	SET @PROC_NAME			= OBJECT_NAME(@@PROCID)
	SET @TIME_NOW			= CURRENT_TIMESTAMP

	-- File Output
	SET @FilePath = 'C:\Users\avinashs\Downloads\'
	SET @OutputFile = 'test.txt'
	SET @bcpCommand = @bcpCommand + @FilePath + @OutputFile + ' -c -t, -T -S'+ @@servername
	

	IF @i_TraceOn <> 0
	BEGIN
		SET @time = CONVERT(VARCHAR, CURRENT_TIMESTAMP - @TIME_NOW, 14)
		PRINT @PROC_NAME + ' - ' + @time 	
	END

	UPDATE [FN-LN-From-CRM]
	SET FirstName = 'Avinash',
	LastName = 'Nested Level 2'
	WHERE ContactID = 1119545;

	IF (COLUMNS_UPDATED()) > 0 
	BEGIN
	    SET @time = CONVERT(VARCHAR, CURRENT_TIMESTAMP - @TIME_NOW, 14)
	    PRINT  @PROC_NAME + ' - ' + @time + 'I am Nested Level 2'
	END

	SET @command = 'BCP "SELECT * FROM SANDBOX.dbo.[FN-LN-From-CRM]" queryout "C:\Users\test'

				+ CONVERT(VARCHAR, YEAR(GETDATE())) + RIGHT('00'

						    + CONVERT(VARCHAR, MONTH(GETDATE())),

							2) + RIGHT('00'

                                                           + CONVERT(VARCHAR, DAY(GETDATE())),

							    2)

					+ '.txt" -c -T -t "|" '
	EXEC xp_cmdshell @command    
	EXEC usp_NestedSP3 1

	RETURN
;

