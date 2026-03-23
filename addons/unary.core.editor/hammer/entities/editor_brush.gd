@tool
class_name BrushEditor extends VMFEntityNode

var target_script = preload("res://unary.core/sources/ecs/EditorOnly.cs")

func _entity_setup(_entity: VMFEntity):
	$MeshInstance3D.set_mesh(get_mesh())
	self.set_script(target_script)
