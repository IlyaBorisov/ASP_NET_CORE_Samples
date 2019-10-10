CREATE PROCEDURE [dbo].[getOrders]
	@start_date DATETIME,
	@end_date DATETIME,
	@part_id INT,
	@sortby INT=0,
	@is_desc BIT=0,
	@offset BIGINT=0,
	@limit BIGINT=NULL	
AS
BEGIN
	WITH dated
	AS
	(
		SELECT 
			id,
			data,			
			nbase,
			city_id,
			street_id,
			fio
		FROM source
		WHERE @start_date<=data_f and data_f<=@end_date and part_id=@part_id
	)
	SELECT 
		id,
		data as date,
		RTRIM(filial.visible_name) as branch,
		RTRIM(city.name) as city,
		RTRIM(street.name) as street,
		RTRIM(fio) as fullname
	FROM dated
	left join filial
		ON filial.nbase=dated.nbase
	left join city
		ON city.city_id=dated.city_id
	left join street
		ON street.street_id=dated.street_id
	ORDER BY
		CASE WHEN @sortby=1 AND @is_desc=0 THEN data END ASC,
		CASE WHEN @sortby=1 AND @is_desc=1 THEN data END DESC,
		CASE WHEN @sortby=2 AND @is_desc=0 THEN filial.visible_name END ASC,
		CASE WHEN @sortby=2 AND @is_desc=1 THEN filial.visible_name END DESC,
		CASE WHEN @sortby=2 AND @is_desc=0 THEN city.name END ASC,
		CASE WHEN @sortby=2 AND @is_desc=1 THEN city.name END DESC,
		CASE WHEN @sortby=2 AND @is_desc=0 THEN street.name END ASC,
		CASE WHEN @sortby=2 AND @is_desc=1 THEN street.name END DESC,
		CASE WHEN @is_desc=0 THEN nz_id END ASC,
		CASE WHEN @is_desc=1 THEN nz_id END DESC
		OFFSET @offset ROWS 
		FETCH NEXT IIF(@limit > 0, @limit, 0x7ffffff) ROWS ONLY
END
