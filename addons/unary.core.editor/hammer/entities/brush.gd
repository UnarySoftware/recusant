@tool
class_name Brush extends VMFEntityNode

var brush_entity = preload("res://unary.core/sources/ecs/BrushEntity.cs")

enum CollisionType 
{
	Disabled = 0,
	Static = 1,
	Dynamic = 2,
}

func _entity_setup(_entity: VMFEntity):
	var mesh = get_mesh();
	mesh.lightmap_unwrap(global_transform, config.import.lightmap_texel_size);

	if !mesh or mesh.get_surface_count() == 0:
		queue_free();
		return;
	
	$MeshInstance3D.set_mesh(mesh);
	
	var geometry_instance : GeometryInstance3D = $MeshInstance3D
	geometry_instance.gi_mode = _entity.data.get("gimode") as GeometryInstance3D.GIMode
	
	var collisions = _entity.data.get("collision") as CollisionType
	
	var group = _entity.data.get("groupname") as String
	$MeshInstance3D.add_to_group.call_deferred(group, true)
	
	if collisions == CollisionType.Disabled:
		$MeshInstance3D/StaticBody3D.queue_free()
		$MeshInstance3D/CharacterBody3D.queue_free()
	else:
		var surface_prop_name = _entity.data.get("surfacepropname")
		$MeshInstance3D/StaticBody3D.set_meta("surface_prop", surface_prop_name)
		if collisions == CollisionType.Static:
			$MeshInstance3D/CharacterBody3D.queue_free()
			$MeshInstance3D/StaticBody3D/CollisionShape3D.shape = mesh.create_trimesh_shape()
			$MeshInstance3D.layers = 1
		else:
			$MeshInstance3D/StaticBody3D.queue_free()
			$MeshInstance3D/CharacterBody3D/CollisionShape3D.shape = mesh.create_convex_shape(true, true)
			$MeshInstance3D.layers = 16384

	self.set_script(brush_entity)
	call_deferred("_ready")
