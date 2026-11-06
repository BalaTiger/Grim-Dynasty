#ifndef GD_OWN_LUAJIT204_BRIDGE_H
#define GD_OWN_LUAJIT204_BRIDGE_H
#include <stdint.h>
#ifdef GD_LJ204_BRIDGE_BUILD
#define GD_LJ204_API __declspec(dllexport)
#else
#define GD_LJ204_API __declspec(dllimport)
#endif
#ifdef __cplusplus
extern "C" {
#endif

/* OWN boundary ABI v1, Windows x64. No original DLL or arbitrary Lua callback.
 * Caller holds the verified Lua module/file and state owner through close,
 * AND the bridge DLL/file plus managed callback delegates/cookie through the
 * return from CompleteClose. Native contexts pin Lua, not their own bridge DLL.
 * Callback implementations return POD status and MUST NOT call Lua, reenter,
 * throw across this ABI, retain borrowed bytes, or dispose the bound state.
 * Native code raises Lua errors only after these callbacks have returned.
 * A published VM supports only this bridge's fixed execution entry and its
 * admitted close. Execution through another entry or coroutine is outside this
 * profile and can fail-fast; the managed owner must reject such use beforehand. */
enum gd_lj204_status {
 GD_LJ204_OK=0, GD_LJ204_INVALID=1, GD_LJ204_CLOSED=2,
 GD_LJ204_BUSY=3, GD_LJ204_MODULE=4, GD_LJ204_MEMORY=5,
 GD_LJ204_LUA_ERROR=6, GD_LJ204_UNSUPPORTED=7,
 GD_LJ204_HOST_ERROR=8, GD_LJ204_WRONG_THREAD=9
};
enum gd_lj204_resolve_flags { GD_LJ204_READ_STATE=1, GD_LJ204_READ_REGISTRY=2 };
/* callback status: 0=resolved/consumed, 1=Unsupported, 2=host exception.
 * Other nonzero values remain host_status and fail as Unsupported. */
typedef int32_t (__cdecl *gd_lj204_resolver)(void *cookie, uint32_t stage_rva,
 uint32_t requested, void **current_state, int32_t *registry_index);
/* Completion-only synchronous POD resolver.0=known live DWORD,1=unknown,
 *2=host error. Borrowed only for that call; never defaults unknown to zero. */
typedef int32_t (__cdecl *gd_lj204_flags_reader)(void *cookie,uint32_t stage_rva,
 uint32_t *flags_dword);
enum gd_lj204_script_read {
 GD_LJ204_SCRIPT_REGISTRY=1,GD_LJ204_SCRIPT_VERSION=2,GD_LJ204_SCRIPT_FLAGS=3,
 GD_LJ204_SCRIPT_UPVALUE_BASE=4,GD_LJ204_SCRIPT_DESCRIPTOR=5
};
/*0 known signed64 value,1 unknown,2 host error. Requested values are separate
 * typed domains: registry/upvalue signed32, version/flags DWORD, descriptorBYTE.
 * Unlike the completion-only reader, this binding/cookie MUST remain rooted
 * through lua_close and CompleteClose: published Script C closures consume it.
 * The descriptor is an explicit OWN observation/input, not inferred disk zero. */
typedef int32_t (__cdecl *gd_lj204_script_reader)(void *cookie,uint32_t stage_rva,
 uint32_t requested,int64_t *value);
/* priorities: print=3 (0F2D00), panic=4 (0F2D20); flags bit0=raw NULL.
 * The status is an OWN adaptation: the observed original call passes one
 * borrowed char* and does not consume its return; its return type is unproved. */
typedef int32_t (__cdecl *gd_lj204_logger)(void *cookie, uint32_t priority,
 const unsigned char *bytes, uint32_t byte_count, uint32_t flags);

typedef struct gd_lj204_result {
 uint32_t struct_bytes, bridge_status;
 int32_t lua_status;
 uint32_t stage_rva;
 uint32_t published_mask, callback_calls, logger_calls, resolver_calls;
 int32_t entry_top, exit_top, failure_local_top, host_status;
 uint32_t flags, fixture_id;
 uint64_t context_token;
} gd_lj204_result; /* sizeof=64; standard x64 packing. */
/* flags: bit0=OWN protected unwind; bit1=error object retained;
 * bit2=known-null hook skipped; bit3=initialized print buffer observed;
 * bit4=the owning caller reported lua_close returned before CompleteClose.
 * bit5=completion's OWN rethrow of the actual retained pcall error AFTER the
 * evidenced normal return0 path and all normal record cleanup completed.
 * bit6=fixture23 actually observed Script and all three Cclosure/upvalue/node identities.
 * Fixture24 only: bit7=Client is boolean, bit8=Client is true,
 * bit9=Server is boolean, bit10=Server is true, bit11=both observations complete.
 * A completed observation with the respective boolean bit clear means nil;
 * other global types are Unsupported, not silently converted to false.
 * published_mask is cumulative successful context operations, NOT a per-call
 * delta (a later partial repeat does not erase previously published fields).
 * failure_local_top is the last measured local protected-frame stack,
 * NOT a claim that native original exception cleanup has been restored. */
enum gd_lj204_fixture {
 GD_LJ204_PRINT_ONE=1,        /* print('OWN callback') */
 GD_LJ204_PRINT_TWO=2,        /* print('first','second') */
 GD_LJ204_PRINT_ZERO=3,       /* print(): uninitialized buffer domain */
 GD_LJ204_TOSTRING_NIL=4,     /* OWN tostring returns nil for all args */
 GD_LJ204_TOSTRING_ERROR=5,   /* OWN tostring raises a real Lua error */
 GD_LJ204_FIRST_NIL=6,        /* first conversion nil: second has leading space */
 GD_LJ204_EMBEDDED_NUL=7,     /* print('A\\000B'): original C-string truncation */
 GD_LJ204_PRINT_GROWTH=8,     /* print(string.rep('x',300)) */
 GD_LJ204_PROTECTED_PANIC=9, /* direct protected wrapper probe, NOT fatal panic */
 GD_LJ204_PANIC_NULL=10,     /* nil -> actual NULL raw-hook argument */
 GD_LJ204_FAIL_LUAGLUE_GLOBAL=11, /* OWN _G.__newindex rejects __luaglue */
 GD_LJ204_FAIL_DUMPEX_GLOBAL=12,  /* OWN _G.__newindex rejects dumpex */
 GD_LJ204_GC_LIST_COLLECT=13,     /* OWN empty list +16B HeapAlloc buffer; actual GC */
 GD_LJ204_GC_OBJECT_COLLECT=14,   /* OWN explicit null payload; actual GC */
 GD_LJ204_GC_LIST_CLOSE=15,       /* As13, defer to actual lua_close */
 GD_LJ204_GC_OBJECT_CLOSE=16,     /* As14, defer to actual lua_close */
 GD_LJ204_GC_UNKNOWN_CLOSE=17,    /* Unregistered userdata; close must record failure */
 GD_LJ204_GC_NONEMPTY_LIST=18,    /* OWN nonnull sentinel, unknown node/vslot0 */
 GD_LJ204_GC_NONNULL_OBJECT=19,   /* OWN nonnull sentinel, unknown type/vslot0 */
 GD_LJ204_GC_FOREIGN_BUFFER=20,   /* OWN empty list, unowned nonnull free target */
 GD_LJ204_FAIL_RECORD_PCALL=21,   /* OWN table metatable rejects first real chunk's assignment */
 GD_LJ204_FAIL_UNKNOWN_CALLBACK=22, /* OWN table hook calls actual print with unresolved host logger */
 GD_LJ204_OBSERVE_SCRIPT=23, /* OWN raw table/Cfunction/upvalue/owned-node identity observation only */
 GD_LJ204_OBSERVE_SERVICE_ROLES=24 /* OWN raw Client/Server nil-or-boolean observation only */
};

/* Separate production receipt. Existing five exports and their64B receipt
 * remain unchanged; fixture_id is never used as a production phase. */
enum gd_lj204_luaglue_phase {
 GD_LJ204_GL_NOT_ENTERED=0, GD_LJ204_GL_VERSION=1,
 GD_LJ204_GL_RECORDS=2, GD_LJ204_GL_PREPARE=3,
 GD_LJ204_GL_LOAD=4, GD_LJ204_GL_PCALL=5,
 GD_LJ204_GL_PUBLISH=6, GD_LJ204_GL_OBSERVE=7,
 GD_LJ204_GL_REGISTRY_BOUNDARY=8
};
typedef struct gd_lj204_luaglue_record_result {
 int32_t prepared_length,load_status,pcall_status;
 int32_t top_before_load,top_after_load,top_after_pcall,top_after_publish;
 int32_t table_field_type,global_type,global_alias_equal;
 uint32_t snapshot_offset,snapshot_bytes,published,prepared,phase,stage_rva;
} gd_lj204_luaglue_record_result; /* sizeof64, all fields4B. */
typedef struct gd_lj204_luaglue_result {
 gd_lj204_result call; /*64B; call.fixture_id=0, lua_status is outer cpcall. */
 uint32_t struct_bytes,phase;
 int32_t current_record,native_return_byte;
 uint32_t prepared_count,loaded_count,executed_count,published_count;
 uint32_t snapshot_bytes,version_publication_mask;
 uint32_t table_observed_count,globals_observed_count;
 gd_lj204_luaglue_record_result records[11];
} gd_lj204_luaglue_result; /* sizeof816, standard x64 Pack8. */
typedef struct gd_lj204_registry_result {
 gd_lj204_result call;
 uint32_t struct_bytes,phase,registry_published_mask,observed_mask;
 int32_t registry_types[4],luaglue_alias;
 uint32_t gc_closure_match_mask;
 int32_t native_return_byte;
 uint32_t reserved;
} gd_lj204_registry_result; /* sizeof112, Pack8. phase0/1publish/2observe/3boundary. */
typedef struct gd_lj204_gc_audit {
 uint32_t struct_bytes,list_entered,list_completed,object_entered,object_completed;
 uint32_t fixture_userdata_created,own_buffer_allocations,own_buffer_free_calls,own_buffer_free_successes;
 uint32_t live_own_buffers,unsupported_callbacks,close_callbacks,fields_unchanged_checks,last_failure_rva;
 int32_t last_callback_return;
 uint32_t reserved;
 uint64_t context_token;
} gd_lj204_gc_audit; /* sizeof72, Pack8. last_callback_return=-1 until observed. */
enum gd_lj204_completion_phase {
 GD_LJ204_COMPLETE_NOT_ENTERED=0,GD_LJ204_COMPLETE_GLUE=1,
 GD_LJ204_COMPLETE_REGISTRY=2,GD_LJ204_COMPLETE_FLAGS=3,
 GD_LJ204_COMPLETE_LOG_BOUNDARY=4,GD_LJ204_COMPLETE_CLEANUP=5,
 GD_LJ204_COMPLETE_RETURNED=6
};
typedef struct gd_lj204_completion_result {
 gd_lj204_luaglue_result glue; /*816; Call covers the ONE outer cpcall. */
 uint32_t struct_bytes,phase,registry_published_mask,registry_observed_mask;
 int32_t registry_types[4],luaglue_alias;
 uint32_t gc_closure_match_mask;
 int32_t flags_known;uint32_t flags_dword;
 int32_t log_returned,cleanup_current_record;
 uint32_t records_constructed;int32_t records_live_at_flag;
 uint32_t normal_cleanup_entered,normal_records_completed;
 uint32_t normal_free_attempts,normal_free_successes,own_free_attempts,own_free_successes;
 uint32_t live_record_allocations,cleanup_trace_count,cleanup_trace[33];
 uint32_t prepared_failure_releases;
} gd_lj204_completion_result; /* sizeof1048, Pack8. */
typedef struct gd_lj204_script_binding_result {
 uint32_t kind,helper_entered,helper_returned,phase;
 int32_t wrapper_mode,wrapper_reference,core_table_index,lookup_is_c;
 uint32_t helper_name_length,helper_name_capacity,helper_allocations,helper_releases;
 uint32_t node_created,userdata_created,node_published,closure_published;
 uint32_t known_node_field_mask;int32_t descriptor_known;
 uint32_t descriptor_byte,stage_rva;
} gd_lj204_script_binding_result; /*80B. kinds0 Load/1 Register/2 Unregister. */
typedef struct gd_lj204_script_result {
 gd_lj204_result call;
 uint32_t struct_bytes,phase,helpers_completed,table_created;
 int32_t table_reference,outer_version;
 uint32_t global_published,reference_released,normal_returned,wrapper_reads;
 int32_t prefix_identity_preserved;uint32_t own_temp_releases;
 uint32_t normal_temp_releases,live_temp_allocations;
 gd_lj204_script_binding_result bindings[3];
} gd_lj204_script_result; /*360B, Pack8. */
typedef struct gd_lj204_script_audit {
 uint32_t struct_bytes,nodes_allocated,nodes_published,destructor_entered,destructor_completed;
 uint32_t name_free_attempts,name_free_successes,node_free_attempts,node_free_successes;
 uint32_t live_nodes,live_node_names,own_unpublished_node_frees,own_unpublished_name_frees;
 uint32_t unsupported_gc,callback_entries,last_failure_rva;
 int32_t last_callback_return;uint32_t close_node_callbacks;
 uint64_t context_token;
} gd_lj204_script_audit; /*80B, Pack8; metadata only, including after actual close. */
enum gd_lj204_service_role { GD_LJ204_ROLE_CLIENT=0,GD_LJ204_ROLE_SERVER=1 };
typedef struct gd_lj204_role_result {
 gd_lj204_result call;
 uint32_t struct_bytes,kind,phase,boolean_pushed;
 int32_t reference,version_known;
 uint32_t version_dword,global_published,reference_released,normal_returned;
 int32_t prefix_identity_preserved;uint32_t reserved;
} gd_lj204_role_result; /*112B, Pack8. */
/* Role phases0 notentered/1 push/2 ref/3 version/4 global/5 unref/6 completed.
 * reference=-1 until actual luaL_ref returns; version_known=-1 unread,0 unknown,
 *1 known. Only known version_dword is meaningful. Each entered true branch is
 * one protected attempt per kind/context. False branches never call this API,
 * never capture a VM and never delete or overwrite an old global. Normal return
 * means only this selected branch completed, NOT full service initialization.
 * global_published records that actual settable returned, not a raw read-back
 * guarantee if a global metatable intercepts the write. Fixture24 supplies an
 * independent raw observation. call.published_mask keeps its original callback-
 * prefix meaning. Prefix observation rules match
 * Script: empty or actual singleton-string identity; no outer stack clearing. */
/* Script receipt phases0 notentered/1 table-ref/2 helpers/3 global/4 unref/5
 * normalreturn. Binding phases0/1 helpername/2 table/3 core/4 node/5 published/
 *6 normalreturn. Signed unobserved fields=-1. known_node_field_mask bits0..6
 * represent vt,name,capacity,length,next,callback,descriptor; padding31..37 is
 * never claimed initialized. Actual positive core_table_index is local to the
 * OWN protected C frame; it is not the original absolute index plus any outer
 * error prefix. Outer stack is never cleared. Prefix identity observation is
 *1 for empty or independently checked singleton-string prefix, -1 otherwise.
 * No arbitrary callback addresses, Script business execution or Initialized58.
 * Normal/helper frees and OWN protected-unwind frees are separate. */
/* Existing prefix APIs remain independent and release their OWN records early.
 * Completion requires a fresh, never-attempted LuaGlue context and retains the
 * SAME11 owned records to the real tail. flags_known=-1 unread/0 unknown/1 read;
 * flags_dword is meaningful only for1. Unobserved signed fields remain-1.
 * cleanup_trace records completed normal releases as recordIndex*3+component,
 * where source=0/chunk=1/name=2. OWN post-error cleanup is counted separately;
 * it never produces a normal-record completion or a native return byte.
 * glue.native_return_byte is1 only after real normal-tail cleanup, or0 after
 * actual record-pcall failure, prepared release and that same cleanup. In that
 * normal0 case the actual error is rethrown AFTER cleanup to transfer it out
 * of the OWN cpcall frame: outer lua_status is nonzero, BridgeStatus=LuaError,
 * flags include bit5 plus protected-unwind/error-object observations. This
 * transfer is not original EH and is distinct from an unproved failure path.
 * prepared_failure_releases counts only the evidenced257774 prepared free. */
/* Record free counters exclude prepared/snapshot scratch. Those are OWN
 * transient storage, except the separately recorded257774 prepared release.
 * An inner pcall error caused by OWN Unsupported/host/memory failure does NOT
 * qualify for original normal0: its exact stop survives with OWN cleanup only. */
/* Fixtures13..20 are admitted at most once each per context. Close fixtures
 * retain their userdata under OWN positive registry keys0x47440000+fixture_id;
 * these test roots are not part of the original four-key registry contract.
 * Audit counters are cumulative OWN observations, not original type-destructor
 * coverage. The only nonnull free target supported is a live buffer explicitly
 * allocated for that exact OWN userdata. No original object vtable is called.
 * A close-internal Lua error can be swallowed; Unsupported/last_failure_rva
 * and the separate CloseResult remain even when lua_close returns normally. */
/* All unobserved signed fields=-1; snapshot_offset=UINT32_MAX until captured.
 * snapshot_bytes is the valid output prefix, not the capacity. Each prepared
 * record owns one exact offset/length interval excluding its appended NUL;
 * consumers hash only those bytes. No terminator is silently counted as source.
 * Prepared/Loaded/Executed/Published count actual completed steps in THIS call.
 * version mask: bit0 table made, bit1 version field stored, bit2 __luaglue
 * stored globally, bit3 __luaglue_version stored globally. No ready-byte store.
 * Record table/global type and alias are explicit OWN raw-read observations,
 * not extra original calls. Unobserved counts/types remain0/-1.
 * A completed supported prefix still stops at25762B with Unsupported and
 * native_return_byte=-1; it is not a completed native2568D0 initialization. */

GD_LJ204_API int32_t __cdecl gd_lj204_attach(void *verified_lua_module,
 void *owned_lua_state, gd_lj204_resolver resolver, gd_lj204_logger logger,
 void *cookie, uint64_t *token);
/* One native cpcall. Resolver reads STATE at5A/70/80/90/A6/B9/C9, REGISTRY
 * at67/9D. A changed state is Unsupported at that step; no hidden snapshot.
 * Prefix mask bits0..6 record successful5A/70/80/90/A6/B9/C9 operations.
 * Nonempty entry stack is outside this bounded initialization profile. */
GD_LJ204_API int32_t __cdecl gd_lj204_publish_prefix(uint64_t token,
 gd_lj204_result *result);
/* Fixed OWN fixture source only. Mutations (e.g. tostring) are not rolled back;
 * tests use independent states. No arbitrary script/path/function interface. */
GD_LJ204_API int32_t __cdecl gd_lj204_execute_fixture(uint64_t token,
 uint32_t fixture_id, gd_lj204_result *result);
/* Exactly the compiled, hash-pinned11 embedded records. No caller-supplied Lua
 * source, script path or production mode flag. snapshot_out is OUTPUT ONLY,
 * capacity must be8192; copying occurs after the native protected call returns.
 * No truncation: unsupported capacity is rejected before any Lua mutation.
 * One protected attempt per context; no implicit replay after partial mutation.
 * The managed caller captures service.Lua00 once at0F2ECF and selects this same
 * context; native execution does not re-read/rebind that captured identity. */
GD_LJ204_API int32_t __cdecl gd_lj204_initialize_luaglue(uint64_t token,
 gd_lj204_luaglue_result *result,unsigned char *snapshot_out,uint32_t capacity);
/* Continue only after this native context recorded the real11-record normal
 * prefix. Captured VM never rebinds to current service.Lua00. Four REGISTRY
 * resolver calls occur at25763F/257662/2576AD/2576F8. No registry rollback.
 * Stops Unsupported at257707, before the unproved4217D8 feature flag. */
GD_LJ204_API int32_t __cdecl gd_lj204_publish_luaglue_registry(uint64_t token,
 gd_lj204_registry_result *result);
/* New full entry; no old partially-returned prefix can be resumed here. The
 * caller captures service.Lua00 once, then this context identity never changes.
 * Snapshot rules match the old8192 output. Flags are read once at257707; bit0
 * set remains Unsupported at257724 until the logger/formatter path is proved.
 * No ready-byte write, Script initialization or arbitrary code input. */
GD_LJ204_API int32_t __cdecl gd_lj204_initialize_luaglue_completion(uint64_t token,
 gd_lj204_flags_reader flags_reader,void *flags_cookie,
 gd_lj204_completion_result *result,unsigned char *snapshot_out,uint32_t capacity);
/* Captures current service VM in the managed owner atEF8C4, then uses this same
 * context. Native local wrapper is reread at each proven site; outerL/ref stay
 * separately captured. One attempt per context; permits an existing outer
 * error prefix. Cold table branch only; live unknowns stop at their read sites.
 * The managed binding roots reader/cookie even after partial failure. */
GD_LJ204_API int32_t __cdecl gd_lj204_initialize_script(uint64_t token,
 gd_lj204_script_reader reader,void *cookie,gd_lj204_script_result *result);
GD_LJ204_API int32_t __cdecl gd_lj204_read_script_audit(uint64_t token,
 gd_lj204_script_audit *audit);
/* Caller tests the original bool before invoking this entry and separately
 * captures service.Lua00 atF2F2E(Client) orF3018(Server). This context never
 * rebinds during the branch. reader/cookie are borrowed synchronously only for
 * this call, using Script reader domains REGISTRY=1 and VERSION=2; they never
 * replace the permanent Script closure reader. Registry/version are reread at
 * original sites. Version2 stops before the unsupported alternate pseudo-index.
 * Unknown/error prefixes retain any actual registry ref/global publication;
 * no invented unref rollback. No providers, Script business or Initialized58. */
GD_LJ204_API int32_t __cdecl gd_lj204_publish_service_role(uint64_t token,
 uint32_t kind,gd_lj204_script_reader reader,void *cookie,gd_lj204_role_result *result);
/* Pure context metadata: no Lua call and no freed userdata dereference.
 * Normal owner thread with no active call, or the admitted close thread after
 * lua_close returned and BEFORE CompleteClose. Caller enforces that close
 * return occurred; native metadata alone cannot observe that external call. */
GD_LJ204_API int32_t __cdecl gd_lj204_read_gc_audit(uint64_t token,
 gd_lj204_gc_audit *audit);
/* No Lua call here. BeginClose rejects active use, permits close-thread takeover
 * and retains all bindings. The one owning runtime then calls lua_close once.
 * CompleteClose is legal ONLY after lua_close returned normally, on that same
 * closing thread. Failed/uncertain close must retain context/module/delegates.
 * Any still-live OWN buffer prevents CompleteClose release: it is quarantined,
 * not silently freed by fallback cleanup or counted as a successful callback.
 * Tokens are never reused within this loaded bridge. Managed owners must also
 * check bridge-instance identity across module reloads (a token is not a public
 * transferable handle). Callbacks/functions are invalid after completion. */
GD_LJ204_API int32_t __cdecl gd_lj204_begin_close(uint64_t token);
/* Return0 means context release completed. close_result separately preserves
 * any callback failure during lua_close, even if Lua swallowed that error.
 * lua_status=-1 means the close-internal catch status could not be observed. */
GD_LJ204_API int32_t __cdecl gd_lj204_complete_close(uint64_t token,
 gd_lj204_result *close_result);
#ifdef __cplusplus
}
#endif
#endif
