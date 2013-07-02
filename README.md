StudentTransfer
===============

學生線上轉學模組

============= 新竹專用數位學生證資料庫同步 DSA Service 規格 =============
轉出
http://163.19.149.20/iccard/ws/trans.asmx?op=tOut
<Request>
  <Name>name</Name>
	<StudentID>std_id</StudentID>
	<TargetSchool>to_sch</TargetSchool>
	<Writer>write_id</Writer>
	<Birthday>birthday</Birthday>
</Request>

轉入
http://163.19.149.20/iccard/ws/trans.asmx?op=tIn
<Request>
	<TargetSchool>to_sch</TargetSchool>
	<Writer>write_id</Writer>
	<StudentID>std_id</StudentID>
	<StudentNumber>std_no</StudentNumber>
	<Grade>sGrade</Grade>
	<ClassName>sClass</ClassName>
</Request>

外縣轉入
http://163.19.149.20/iccard/ws/trans.asmx?op=tForeignIn
<Request>
	<SourceSchool>from_sch</SourceSchool>
	<TargetSchool>to_sch</TargetSchool>
	<Writer>write_id</Writer>
	<StudentID>std_id</StudentID>
	<Name>name</Name>
	<StudentNumber>std_no</StudentNumber>
	<Grade>sGrade</Grade>
	<ClassName>sClass</ClassName>
	<Birthday>birthday</Birthday>
</Request>
