ALTER PROCEDURE [dbo].[GetMailMetrics](
	@userId int NULL
,	@startDate datetime
,	@endDate datetime
)
AS
	WITH messages (id, message_date, sender_id, answered_in)
	AS
	(	
		SELECT
			id
		,	message_date
		,	sender_id
		,	answered_in
		FROM
			mail_message
		WHERE
			message_date BETWEEN @startDate AND @endDate
	)
	SELECT
			U.user_id

		-- If user doesn't have a name we use email instead
		,	CASE
				WHEN ISNULL(U.first_name, '') = '' AND ISNULL(U.last_name, '') = '' THEN U.primary_email
				ELSE TRIM(ISNULL(U.first_name, '') + ' ' + ISNULL(U.last_name, ''))
			END AS employee_name
		
		-- Received Messages Total
		,	(SELECT
					COUNT(user_id)
				FROM
					messages M
				INNER JOIN
					mail_message_recipient R ON M.id = R.mail_message_id
				WHERE
					R.user_id = U.user_id
			) AS received_total

		-- Answered Messages Total
		,	(SELECT
					COUNT(id)
				FROM
					messages M
				WHERE
					M.sender_id = U.user_id
				AND
					M.answered_in IS NOT NULL
			) AS answered_total

		---- Response Time 25 Percentile
		,	ISNULL((SELECT TOP 1
					PERCENTILE_CONT(0.25) 
						WITHIN GROUP (ORDER BY answered_in) OVER ()
				FROM
					messages M
				WHERE
					M.sender_id = U.user_id
			), 0) AS response_Q1
		
		-- Response Time Median
		,	ISNULL((SELECT TOP 1
					PERCENTILE_CONT(0.5) 
						WITHIN GROUP (ORDER BY answered_in) OVER ()
				FROM
					messages M
				WHERE
					M.sender_id = U.user_id
			), 0) AS response_median
		
		-- Response Time 75 Percentile
		,	ISNULL((SELECT TOP 1
					PERCENTILE_CONT(0.75) 
						WITHIN GROUP (ORDER BY answered_in) OVER ()
				FROM
					messages M
				WHERE
					M.sender_id = U.user_id
			), 0) AS response_Q3

		-- Sent Messages Total
		,	(SELECT
					COUNT(id)
				FROM
					messages M
				WHERE
					M.sender_id = U.user_id
			) AS sent_total
		FROM
			[user] U
		INNER JOIN
			bbemployee E ON U.user_id = E.employee_id
		WHERE
			U.user_id = ISNULL(@userId, U.user_id)
		AND
			E.employment_status_id NOT IN (5, 6, 7) -- Active employees only
		ORDER BY
			employee_name ASC;