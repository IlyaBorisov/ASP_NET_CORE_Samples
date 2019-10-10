ALTER PROCEDURE [dbo].[API_TableCount_Uc]
	@start_date DATETIME,
	@end_date DATETIME,
	@part_id int
AS
begin
	declare 
		@all int,
		@completed int,
		@canceled int,
		@rejected int,
		@inwork int

	select *
    into #partorders
    from Orders
    where @start_date<=CreatedDate and CreatedDate<=@end_date and SourcePartID=@part_id

    select 
        @canceled=[9]+[10],
        @rejected=[11]+[12]+[13]+[14]+[15]+[16]
    from 
    (select CancelReasonId
    from #partorders
    where CancelReasonId is not null)
    as basis
    pivot
    (
        count(CancelReasonId) for CancelReasonId in ([9],[10],[11],[12],[13],[14],[15],[16])
    ) as pvt

    select 
		@completed=[4]+[5]+[7]+[8],
		@inwork=[1]+[2]+[3]
	from
	(select StatusId
	from #partorders
	where CancelReasonID is null)
	as basis
	pivot
	(
		count(StatusId) FOR StatusId in ([1],[2],[3],[4],[5],[7],[8])
	) AS pvt

	SELECT 
		isnull(@completed,0)+isnull(@inwork,0)+isnull(@rejected,0)+isnull(@canceled,0) as _all,
		isnull(@completed,0) as completed,
		isnull(@inwork,0) as inwork,
		isnull(@rejected,0) as rejected,
		isnull(@canceled,0) as canceled
end
