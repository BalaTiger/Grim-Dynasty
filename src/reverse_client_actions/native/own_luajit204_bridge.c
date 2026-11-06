/* OWN platform boundary for the evidenced callback and LuaGlue prefixes.
 * No original binary is loaded. Original exception cleanup, nonempty object
 * destructors, logging-on formatting and full service initialization remain
 * unsupported. Windows x64 C; explicit logging-off LuaGlue completion only. */
#define GD_LJ204_BRIDGE_BUILD
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#define _WIN32_WINNT 0x0602
#include <windows.h>
#include <stdint.h>
#include <string.h>
#include <wchar.h>
#include "lua.h"
#include "lauxlib.h"
#include "own_luajit204_bridge.h"
#include "own_luaglue_embedded_data.h"

typedef char gd_require_x64[(sizeof(void *)==8)?1:-1];
typedef char gd_require_result64[(sizeof(gd_lj204_result)==64)?1:-1];
typedef char gd_require_lua51[(LUA_VERSION_NUM==501)?1:-1];
typedef char gd_require_gl_record64[(sizeof(gd_lj204_luaglue_record_result)==64)?1:-1];
typedef char gd_require_gl_result816[(sizeof(gd_lj204_luaglue_result)==816)?1:-1];
typedef char gd_require_registry112[(sizeof(gd_lj204_registry_result)==112)?1:-1];
typedef char gd_require_gc_audit72[(sizeof(gd_lj204_gc_audit)==72)?1:-1];
typedef char gd_require_completion1048[(sizeof(gd_lj204_completion_result)==1048)?1:-1];
typedef char gd_require_script360[(sizeof(gd_lj204_script_result)==360)?1:-1];
typedef char gd_require_script_binding80[(sizeof(gd_lj204_script_binding_result)==80)?1:-1];
typedef char gd_require_script_audit80[(sizeof(gd_lj204_script_audit)==80)?1:-1];
typedef char gd_require_role112[(sizeof(gd_lj204_role_result)==112)?1:-1];
#define GD_MAX_BYTES (1024u*1024u)
#define GD_FLAG_UNWIND 1u
#define GD_FLAG_ERROR_OBJECT 2u
#define GD_FLAG_NULL_HOOK 4u
#define GD_FLAG_BUFFER_READY 8u
#define GD_FLAG_CLOSE_RETURNED 16u
#define GD_FLAG_NORMAL_ZERO_TRANSFER 32u
#define GD_FLAG_SCRIPT_OBSERVED 64u
#define GD_FLAG_CLIENT_BOOLEAN 128u
#define GD_FLAG_CLIENT_TRUE 256u
#define GD_FLAG_SERVER_BOOLEAN 512u
#define GD_FLAG_SERVER_TRUE 1024u
#define GD_FLAG_ROLES_OBSERVED 2048u

typedef struct gd_api {
 int (__cdecl *gettop)(lua_State *);
 void (__cdecl *settop)(lua_State *,int);
 void (__cdecl *pushvalue)(lua_State *,int);
 void (__cdecl *pushlightuserdata)(lua_State *,void *);
 void (__cdecl *pushcclosure)(lua_State *,lua_CFunction,int);
 void (__cdecl *setfield)(lua_State *,int,const char *);
 void (__cdecl *getfield)(lua_State *,int,const char *);
 void *(__cdecl *touserdata)(lua_State *,int);
 const char *(__cdecl *tolstring)(lua_State *,int,size_t *);
 void (__cdecl *call)(lua_State *,int,int);
 int (__cdecl *cpcall)(lua_State *,lua_CFunction,void *);
 int (__cdecl *loadbuffer)(lua_State *,const char *,size_t,const char *);
 int (__cdecl *error)(lua_State *);
 void (__cdecl *pushlstring)(lua_State *,const char *,size_t);
 void (__cdecl *pushnil)(lua_State *);
 lua_CFunction (__cdecl *atpanic)(lua_State *,lua_CFunction);
 void (__cdecl *createtable)(lua_State *,int,int);
 void (__cdecl *pushstring)(lua_State *,const char *);
 int (__cdecl *pcall)(lua_State *,int,int,int);
 int (__cdecl *type)(lua_State *,int);
 void (__cdecl *rawget)(lua_State *,int);
 int (__cdecl *rawequal)(lua_State *,int,int);
 void (__cdecl *rawgeti)(lua_State *,int,int);
 void (__cdecl *rawseti)(lua_State *,int,int);
 void *(__cdecl *newuserdata)(lua_State *,size_t);
 int (__cdecl *setmetatable)(lua_State *,int);
 lua_CFunction (__cdecl *tocfunction)(lua_State *,int);
 int (__cdecl *gc)(lua_State *,int,int);
 int (__cdecl *ref)(lua_State *,int);
 void (__cdecl *unref)(lua_State *,int,int);
 void (__cdecl *rawset)(lua_State *,int);
 void (__cdecl *settable)(lua_State *,int);
 int (__cdecl *getmetatable)(lua_State *,int);
 int (__cdecl *iscfunction)(lua_State *,int);
 int (__cdecl *isstring)(lua_State *,int);
 int (__cdecl *isnumber)(lua_State *,int);
 const char *(__cdecl *getupvalue)(lua_State *,int,int);
 void (__cdecl *pushboolean)(lua_State *,int);
 int (__cdecl *toboolean)(lua_State *,int);
} gd_api;

/* OWN storage of the evidenced records, not the original80B allocator ABI. */
typedef struct gd_gl_owned_record {
 unsigned char *name,*chunk,*source;
 uint32_t name_length,chunk_length,source_length;
 unsigned char publish;
} gd_gl_owned_record;
typedef struct gd_gc_own_userdata {
 void *userdata,*buffer,*initial_payload,*initial_head;
 uint32_t kind,used,admitted,finalizer_seen,finalized,buffer_live;
 uint32_t script_index,script_epoch;
} gd_gc_own_userdata;
typedef struct gd_script_node_image {
 const void *vtable;unsigned char *name;uint64_t capacity,length;
 void *next;const void *callback;unsigned char descriptor;
 unsigned char unobserved_padding[7];
} gd_script_node_image;
typedef char gd_require_script_node56[(sizeof(gd_script_node_image)==0x38)?1:-1];
typedef struct gd_script_owned_node {
 gd_script_node_image *node;void *userdata;unsigned char *name;
 uint32_t epoch,kind,node_live,name_live,published,destructor_seen;
} gd_script_owned_node;

typedef struct gd_context {
 struct gd_context *next;
 uint64_t token;
 HMODULE module;
 lua_State *state;
 gd_api api;
 gd_lj204_resolver resolver;
 gd_lj204_logger logger;
 void *cookie;
 DWORD owner_thread,close_thread;
 int active,closing,callback_depth;
 uint32_t published;
 gd_lj204_result *result;
 gd_lj204_result close_result;
 unsigned char *scratch;
 size_t capacity,length;
 int initialized;
 int luaglue_attempted;
 gd_lj204_luaglue_result *gl_result;
 gd_gl_owned_record gl_records[11];
 unsigned char *gl_prepared,*gl_snapshot;
 size_t gl_capacity,gl_length;
 int luaglue_completed,registry_attempted,gc_depth;
 uint32_t registry_published,gc_fixture_mask;
 gd_lj204_registry_result *registry_result;
 gd_lj204_gc_audit gc_audit;
 gd_gc_own_userdata gc_userdata[8];
 gd_lj204_completion_result *completion_result;
 gd_lj204_flags_reader completion_flags;
 void *completion_cookie;
 uint32_t gl_record_live[11];
 int gl_full_storage,gl_release_failed;
 gd_lj204_script_reader script_reader;void *script_cookie;
 gd_lj204_script_result *script_result;gd_lj204_script_audit script_audit;
 gd_script_owned_node script_nodes[3];unsigned char *script_temp[4];
 uint32_t script_epoch;int script_attempted,script_cleanup_failed,script_binding_index;
 gd_lj204_script_reader role_reader;void *role_cookie;
 gd_lj204_role_result *role_result;uint32_t role_attempted_mask;
} gd_context;

static SRWLOCK contexts_lock=SRWLOCK_INIT;
static gd_context *contexts;
static uint64_t next_token=1;
static __declspec(thread) gd_context *active_raw_context;
static const wchar_t own_lua_path[]=L"C:\\Users\\zhuzi\\Documents\\Grim-Dynasty\\.tooling\\luajit204-build-run1\\source\\src\\lua51.dll";
static int __cdecl native_print(lua_State *L);
static int __cdecl native_panic(lua_State *L);
static int __cdecl native_gc_list(lua_State *L);
static int __cdecl native_gc_object(lua_State *L);
static int gc_fixture(gd_context *c,lua_State *L,uint32_t fixture);
static int script_gc_object(gd_context *c,lua_State *L,gd_gc_own_userdata *entry,void *object);
static int script_entry_has_resources(gd_context *c,gd_gc_own_userdata *entry);
static int script_observe_fixture(gd_context *c,lua_State *L);
static int role_observe_fixture(gd_context *c,lua_State *L);
static void __cdecl raw_print(char *text);
static void __cdecl raw_panic(char *text);
/* Exact registry-key.bin5 bytes; SHA2566b3cc554d45a56ed43995cc307f4481a80680a993cd06b4ecfef70986c17997e. */
static const char own_gc_key[]={0x5f,0x5f,0x67,0x63,0};

static gd_context *find_token(uint64_t token)
{
 gd_context *c;for(c=contexts;c;c=c->next)if(c->token==token)return c;return NULL;
}
static void init_result(gd_lj204_result *r,uint64_t token,uint32_t fixture)
{
 memset(r,0,sizeof(*r));r->struct_bytes=(uint32_t)sizeof(*r);
 r->context_token=token;r->fixture_id=fixture;r->failure_local_top=-1;
 r->entry_top=-1;r->exit_top=-1;r->lua_status=-1; /* Not observed/invoked yet. */
}
static void stage(gd_context *c,lua_State *L,uint32_t at)
{
 c->result->stage_rva=at;c->result->failure_local_top=c->api.gettop(L);
}
static int fail(gd_context *c,lua_State *L,uint32_t status,const char *text)
{
 c->result->bridge_status=status;c->result->failure_local_top=c->api.gettop(L);
 if(c->closing)c->result->lua_status=-1; /* Close's internal catcher is not observed. */
 c->api.pushlstring(L,text,strlen(text));return c->api.error(L);
}
static void clear_scratch(gd_context *c)
{
 if(c->scratch)HeapFree(GetProcessHeap(),0,c->scratch);
 c->scratch=NULL;c->capacity=0;c->length=0;c->initialized=0;
}
static void *raw_address(void (__cdecl *fn)(char *))
{
 void *p=NULL;memcpy(&p,&fn,sizeof(p));return p;
}
static gd_context *callback_context(lua_State *L)
{
 gd_context *c;
 AcquireSRWLockShared(&contexts_lock);
 for(c=contexts;c;c=c->next)if(c->state==L)break;
 ReleaseSRWLockShared(&contexts_lock);
 /* The typed owner admits callbacks only within an active native call or
  * its exclusive close. Reaching a freed/unowned VM is an invariant fault,
  * not a Lua success or a recoverable memory-access exception. */
 if(!c||(!c->active&&!c->closing)||
    GetCurrentThreadId()!=(c->closing?c->close_thread:c->owner_thread)){
  RaiseFailFastException(NULL,NULL,0);return NULL;
 }
 return c;
}
static void enter_callback(gd_context *c,lua_State *L,uint32_t at)
{
 stage(c,L,at);++c->result->callback_calls;
 if(c->closing){fail(c,L,GD_LJ204_UNSUPPORTED,"OWN closing callback is unsupported");return;}
 if(c->callback_depth!=0){fail(c,L,GD_LJ204_UNSUPPORTED,"OWN nested callback scratch is unsupported");return;}
 c->callback_depth=1;active_raw_context=c;
}
static int32_t resolve(gd_context *c,lua_State *L,uint32_t at,uint32_t flags)
{
 void *current=NULL;int32_t registry=0,answer;
 stage(c,L,at);++c->result->resolver_calls;
 answer=c->resolver(c->cookie,at,flags,&current,&registry);
 /* No Lua operation takes place until the managed POD resolver has returned. */
 if(answer!=0){c->result->host_status=answer;fail(c,L,answer==2?GD_LJ204_HOST_ERROR:GD_LJ204_UNSUPPORTED,"OWN live resolver unavailable");return 0;}
 if((flags&GD_LJ204_READ_STATE)&&current!=(void *)L){fail(c,L,GD_LJ204_UNSUPPORTED,"OWN live service changed Lua identity");return 0;}
 if((flags&GD_LJ204_READ_REGISTRY)&&registry!=LUA_REGISTRYINDEX){fail(c,L,GD_LJ204_UNSUPPORTED,"OWN registry index is outside the fixed Lua51 profile");return 0;}
 return registry;
}
static void send_raw(gd_context *c,char *text,uint32_t priority)
{
 size_t length=0;int32_t answer;lua_State *L=c->state;
 if(text){while(length<GD_MAX_BYTES&&text[length]!='\0')++length;
  if(length==GD_MAX_BYTES){fail(c,L,GD_LJ204_UNSUPPORTED,"OWN raw logger byte limit");return;}}
 ++c->result->logger_calls;
 answer=c->logger(c->cookie,priority,(const unsigned char *)text,(uint32_t)length,text?0u:1u);
 /* In particular, lua_error is not invoked while a reverse-P/Invoke frame
  * remains on the stack. The host may not invoke Lua or throw across its ABI. */
 if(answer!=0){c->result->host_status=answer;
  fail(c,L,answer==2?GD_LJ204_HOST_ERROR:GD_LJ204_UNSUPPORTED,"OWN logger behavior is unavailable");}
}
static void __cdecl raw_print(char *text){send_raw(active_raw_context,text,3);}
static void __cdecl raw_panic(char *text){send_raw(active_raw_context,text,4);}
static void dispatch_hook(gd_context *c,lua_State *L,void *hook,char *text,int panic)
{
 if(hook==NULL){c->result->flags|=GD_FLAG_NULL_HOOK;return;}
 /* A registry lightuserdata is not sufficient proof of a callable address. */
 if(hook!=(panic?raw_address(raw_panic):raw_address(raw_print))){
  fail(c,L,GD_LJ204_UNSUPPORTED,"OWN foreign or stale raw hook identity");return;
 }
 if(panic)raw_panic(text);else raw_print(text);
}
static void grow(gd_context *c,lua_State *L,size_t minimum,size_t copy_bytes)
{
 unsigned char *next;size_t capacity;
 if(minimum>GD_MAX_BYTES||minimum>SIZE_MAX/2){fail(c,L,GD_LJ204_UNSUPPORTED,"OWN print allocation limit");return;}
 capacity=minimum*2;if(capacity<8)capacity=8;
 next=(unsigned char *)HeapAlloc(GetProcessHeap(),0,capacity);
 if(!next){fail(c,L,GD_LJ204_MEMORY,"OWN print allocation failed");return;}
 if(copy_bytes)memcpy(next,c->scratch,copy_bytes);
 HeapFree(GetProcessHeap(),0,c->scratch);c->scratch=next;c->capacity=capacity;
}
static int __cdecl native_print(lua_State *L)
{
 gd_context *c=callback_context(L);int count,index,registry;const char *s;
 size_t len,new_length;void *hook;
 if(!c)return 0;enter_callback(c,L,0x259d80);
 stage(c,L,0x259da8);count=c->api.gettop(L);
 stage(c,L,0x259dc1);c->api.getfield(L,LUA_GLOBALSINDEX,"tostring");
 stage(c,L,0x259de1);c->scratch=(unsigned char *)HeapAlloc(GetProcessHeap(),0,256);
 if(!c->scratch)return fail(c,L,GD_LJ204_MEMORY,"OWN print allocation failed");
 c->capacity=256;c->length=0;c->initialized=0; /* Deliberately no guessed NUL. */
 for(index=1;index<=count;++index){
  stage(c,L,0x259e16);c->api.pushvalue(L,-1);
  stage(c,L,0x259e22);c->api.pushvalue(L,index);
  stage(c,L,0x259e33);c->api.call(L,1,1); /* Native tostring may raise. */
  stage(c,L,0x259e42);s=c->api.tolstring(L,-1,NULL);
  if(s){
   if(index>1){
    if(c->capacity<c->length+2)grow(c,L,c->length+2,c->length?c->length+1:0);
    c->scratch[c->length++]=' ';c->scratch[c->length]=0;c->initialized=1;
   }
   for(len=0;len<GD_MAX_BYTES&&s[len]!='\0';++len){}
   if(len==GD_MAX_BYTES||len>GD_MAX_BYTES-c->length)return fail(c,L,GD_LJ204_UNSUPPORTED,"OWN print byte limit");
   new_length=c->length+len;
   if(c->capacity<new_length+1)grow(c,L,new_length+1,c->length);
   memcpy(c->scratch+c->length,s,len+1);c->length=new_length;c->initialized=1;
  }
  stage(c,L,0x259f45);c->api.settop(L,-2);
 }
 registry=resolve(c,L,0x259f67,GD_LJ204_READ_REGISTRY);
 c->api.getfield(L,registry,"__ls_printHook");
 stage(c,L,0x259f73);hook=c->api.touserdata(L,-1);
 stage(c,L,0x259f81);
 if(hook&&!c->initialized)return fail(c,L,GD_LJ204_UNSUPPORTED,"OWN print buffer has no evidenced terminator");
 if(c->initialized)c->result->flags|=GD_FLAG_BUFFER_READY;
 dispatch_hook(c,L,hook,(char *)c->scratch,0);
 stage(c,L,0x259f8b);c->api.settop(L,-3);
 clear_scratch(c);c->callback_depth=0;return 0;
}
static int __cdecl native_panic(lua_State *L)
{
 gd_context *c=callback_context(L);const char *text;int registry;void *hook;
 if(!c)return 0;enter_callback(c,L,0x259fc0);
 stage(c,L,0x259fd3);text=c->api.tolstring(L,-1,NULL);
 registry=resolve(c,L,0x259fd9,GD_LJ204_READ_REGISTRY);
 stage(c,L,0x259fec);c->api.getfield(L,registry,"__ls_panicHook");
 stage(c,L,0x259ff8);hook=c->api.touserdata(L,-1);
 stage(c,L,0x25a006);dispatch_hook(c,L,hook,(char *)text,1);
 stage(c,L,0x25a010);c->api.settop(L,-2);c->callback_depth=0;return 0;
}
static int __cdecl publish_thunk(lua_State *L)
{
 gd_context *c=active_raw_context;int registry;
 c->api.settop(L,0); /* Remove cpcall's OWN context argument. */
 resolve(c,L,0x0f2e5a,GD_LJ204_READ_STATE);
 c->api.pushlightuserdata(L,raw_address(raw_panic));c->published|=1u;
 registry=resolve(c,L,0x0f2e67,GD_LJ204_READ_REGISTRY);
 resolve(c,L,0x0f2e70,GD_LJ204_READ_STATE);
 c->api.setfield(L,registry,"__ls_panicHook");c->published|=2u;
 resolve(c,L,0x0f2e80,GD_LJ204_READ_STATE);
 c->api.atpanic(L,native_panic);c->published|=4u;
 resolve(c,L,0x0f2e90,GD_LJ204_READ_STATE);
 c->api.pushlightuserdata(L,raw_address(raw_print));c->published|=8u;
 registry=resolve(c,L,0x0f2e9d,GD_LJ204_READ_REGISTRY);
 resolve(c,L,0x0f2ea6,GD_LJ204_READ_STATE);
 c->api.setfield(L,registry,"__ls_printHook");c->published|=16u;
 resolve(c,L,0x0f2eb9,GD_LJ204_READ_STATE);
 c->api.pushcclosure(L,native_print,0);c->published|=32u;
 resolve(c,L,0x0f2ec9,GD_LJ204_READ_STATE);
 c->api.setfield(L,LUA_GLOBALSINDEX,"print");c->published|=64u;
 return 0;
}
static const char *fixture_source(uint32_t id)
{
 switch(id){
 case GD_LJ204_PRINT_ONE:return "print('OWN callback')";
 case GD_LJ204_PRINT_TWO:return "print('first','second')";
 case GD_LJ204_PRINT_ZERO:return "print()";
 case GD_LJ204_TOSTRING_NIL:return "tostring=function() return nil end; print('x','y')";
 case GD_LJ204_TOSTRING_ERROR:return "tostring=function() error('OWN tostring failure') end; print('x')";
 case GD_LJ204_FIRST_NIL:return "local old=tostring; tostring=function(v) if v=='first' then return nil end return old(v) end; print('first','second')";
 case GD_LJ204_EMBEDDED_NUL:return "print('A\\000B')";
 case GD_LJ204_PRINT_GROWTH:return "print(string.rep('x',300))";
 case GD_LJ204_FAIL_LUAGLUE_GLOBAL:return "setmetatable(_G,{__newindex=function(t,k,v) if k=='__luaglue' then error('OWN __luaglue publication failure') end rawset(t,k,v) end})";
 case GD_LJ204_FAIL_DUMPEX_GLOBAL:return "setmetatable(_G,{__newindex=function(t,k,v) if k=='dumpex' then error('OWN dumpex publication failure') end rawset(t,k,v) end})";
 case GD_LJ204_FAIL_RECORD_PCALL:return "setmetatable(_G,{__newindex=function(t,k,v) if k=='__luaglue' then setmetatable(v,{__newindex=function() error('OWN LuaGlue record execution failure') end}) end rawset(t,k,v) end})";
 case GD_LJ204_FAIL_UNKNOWN_CALLBACK:return "setmetatable(_G,{__newindex=function(t,k,v) if k=='__luaglue' then setmetatable(v,{__newindex=function() print('OWN LuaGlue unknownlogger') end}) end rawset(t,k,v) end})";
 default:return NULL;
 }
}
static int __cdecl fixture_thunk(lua_State *L)
{
 gd_context *c=active_raw_context;const char *source;int status;
 c->api.settop(L,0);
 if(c->result->fixture_id==GD_LJ204_OBSERVE_SCRIPT)return script_observe_fixture(c,L);
 if(c->result->fixture_id==GD_LJ204_OBSERVE_SERVICE_ROLES)return role_observe_fixture(c,L);
 if(c->result->fixture_id>=GD_LJ204_GC_LIST_COLLECT&&c->result->fixture_id<=GD_LJ204_GC_FOREIGN_BUFFER)return gc_fixture(c,L,c->result->fixture_id);
 if(c->result->fixture_id==GD_LJ204_PROTECTED_PANIC||c->result->fixture_id==GD_LJ204_PANIC_NULL){
  /* This tests wrapper logic beneath cpcall. It never triggers an actual
   * unprotected panic (LuaJIT terminates after such a panic returns). */
  c->api.pushcclosure(L,native_panic,0);
  if(c->result->fixture_id==GD_LJ204_PANIC_NULL)c->api.pushnil(L);
  else c->api.pushlstring(L,"OWN panic probe",15);
  c->api.call(L,1,0);return 0;
 }
 source=fixture_source(c->result->fixture_id);
 if(!source)return fail(c,L,GD_LJ204_INVALID,"OWN fixture is not allowlisted");
 status=c->api.loadbuffer(L,source,strlen(source),"@OWN-fixed-callback-fixture");
 if(status!=0)return c->api.error(L); /* Re-raise only within native cpcall. */
 c->api.call(L,0,0);return 0;
}
static int module_path_matches(HMODULE module)
{
 wchar_t path[32768];DWORD n=GetModuleFileNameW(module,path,32768);
 return n!=0&&n<32768&&_wcsicmp(path,own_lua_path)==0;
}
static FARPROC own_symbol(HMODULE module,const char *name)
{
 FARPROC proc=GetProcAddress(module,name);HMODULE owner=NULL;void *address=NULL;
 if(!proc)return NULL;memcpy(&address,&proc,sizeof(address));
 if(!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
    (LPCWSTR)address,&owner)||owner!=module)return NULL;
 return proc;
}
static int bind_api(gd_context *c)
{
 FARPROC symbol;
#define BIND(member,name) do{symbol=own_symbol(c->module,name);if(!symbol)return 0;memcpy(&c->api.member,&symbol,sizeof(symbol));}while(0)
 BIND(gettop,"lua_gettop");BIND(settop,"lua_settop");BIND(pushvalue,"lua_pushvalue");
 BIND(pushlightuserdata,"lua_pushlightuserdata");BIND(pushcclosure,"lua_pushcclosure");
 BIND(setfield,"lua_setfield");BIND(getfield,"lua_getfield");BIND(touserdata,"lua_touserdata");
 BIND(tolstring,"lua_tolstring");BIND(call,"lua_call");BIND(cpcall,"lua_cpcall");
 BIND(loadbuffer,"luaL_loadbuffer");BIND(error,"lua_error");BIND(pushlstring,"lua_pushlstring");
 BIND(pushnil,"lua_pushnil");BIND(atpanic,"lua_atpanic");
 BIND(createtable,"lua_createtable");BIND(pushstring,"lua_pushstring");
 BIND(pcall,"lua_pcall");BIND(type,"lua_type");BIND(rawget,"lua_rawget");BIND(rawequal,"lua_rawequal");
 BIND(rawgeti,"lua_rawgeti");BIND(rawseti,"lua_rawseti");BIND(newuserdata,"lua_newuserdata");
 BIND(setmetatable,"lua_setmetatable");BIND(tocfunction,"lua_tocfunction");BIND(gc,"lua_gc");
 BIND(ref,"luaL_ref");BIND(unref,"luaL_unref");BIND(rawset,"lua_rawset");BIND(settable,"lua_settable");
 BIND(getmetatable,"lua_getmetatable");BIND(iscfunction,"lua_iscfunction");
 BIND(isstring,"lua_isstring");BIND(isnumber,"lua_isnumber");
 BIND(getupvalue,"lua_getupvalue");
 BIND(pushboolean,"lua_pushboolean");BIND(toboolean,"lua_toboolean");
#undef BIND
 return own_symbol(c->module,"luaJIT_version_2_0_4")!=NULL;
}

int32_t __cdecl gd_lj204_attach(void *verified_lua_module,void *owned_lua_state,
 gd_lj204_resolver resolver,gd_lj204_logger logger,void *cookie,uint64_t *token)
{
 gd_context *c,*cursor;HMODULE pinned=NULL;int32_t status=GD_LJ204_OK;
 if(!token||!verified_lua_module||!owned_lua_state||!resolver||!logger)return GD_LJ204_INVALID;
 *token=0;
 /* Identity/hash/file-sharing provenance is supplied by the closed managed
  * runtime owner; raw VM pointer provenance is not discoverable from Lua's ABI.
  * This layer additionally verifies the exact HMODULE, path and export owners. */
 if(!module_path_matches((HMODULE)verified_lua_module))return GD_LJ204_MODULE;
 if(!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS,(LPCWSTR)verified_lua_module,&pinned))return GD_LJ204_MODULE;
 if(pinned!=(HMODULE)verified_lua_module){FreeLibrary(pinned);return GD_LJ204_MODULE;}
 c=(gd_context *)HeapAlloc(GetProcessHeap(),HEAP_ZERO_MEMORY,sizeof(*c));
 if(!c){FreeLibrary(pinned);return GD_LJ204_MEMORY;}
 c->module=pinned;c->state=(lua_State *)owned_lua_state;c->resolver=resolver;c->logger=logger;c->cookie=cookie;
 c->owner_thread=GetCurrentThreadId();
 c->gc_audit.struct_bytes=(uint32_t)sizeof(c->gc_audit);c->gc_audit.last_callback_return=-1;
 c->script_audit.struct_bytes=(uint32_t)sizeof(c->script_audit);c->script_audit.last_callback_return=-1;c->script_binding_index=-1;
 if(!bind_api(c)){FreeLibrary(pinned);HeapFree(GetProcessHeap(),0,c);return GD_LJ204_MODULE;}
 AcquireSRWLockExclusive(&contexts_lock);
 for(cursor=contexts;cursor;cursor=cursor->next)if(cursor->state==c->state){status=GD_LJ204_BUSY;break;}
 if(status==GD_LJ204_OK){
  if(next_token==0||next_token==UINT64_MAX)status=GD_LJ204_MEMORY;
  else{c->token=next_token++;c->gc_audit.context_token=c->token;c->script_audit.context_token=c->token;c->next=contexts;contexts=c;*token=c->token;}
 }
 ReleaseSRWLockExclusive(&contexts_lock);
 if(status!=GD_LJ204_OK){FreeLibrary(pinned);HeapFree(GetProcessHeap(),0,c);}
 return status;
}
static int32_t run(uint64_t token,uint32_t fixture,gd_lj204_result *result)
{
 gd_context *c,*prior;int status;int32_t answer=GD_LJ204_OK;
 if(!result)return GD_LJ204_INVALID;init_result(result,token,fixture);
 AcquireSRWLockExclusive(&contexts_lock);c=find_token(token);
 if(!c||c->closing)answer=GD_LJ204_CLOSED;
 else if(c->active||active_raw_context)answer=GD_LJ204_BUSY;
 else if(c->owner_thread!=GetCurrentThreadId())answer=GD_LJ204_WRONG_THREAD;
 else if(fixture>GD_LJ204_OBSERVE_SERVICE_ROLES)answer=GD_LJ204_INVALID;
 else if(fixture!=0&&c->published!=127u)answer=GD_LJ204_UNSUPPORTED;
 else if(fixture>=GD_LJ204_GC_LIST_COLLECT&&fixture<=GD_LJ204_GC_FOREIGN_BUFFER&&c->registry_published!=15u)answer=GD_LJ204_UNSUPPORTED;
 else if(fixture>=GD_LJ204_GC_LIST_COLLECT&&fixture<=GD_LJ204_GC_FOREIGN_BUFFER&&(c->gc_fixture_mask&(1u<<(fixture-GD_LJ204_GC_LIST_COLLECT))))answer=GD_LJ204_UNSUPPORTED;
 else{c->active=1;c->result=result;}
 ReleaseSRWLockExclusive(&contexts_lock);
 if(answer!=GD_LJ204_OK){result->bridge_status=(uint32_t)answer;return answer;}
 prior=active_raw_context;active_raw_context=c;
 result->entry_top=c->api.gettop(c->state);
 if(result->entry_top!=0){result->bridge_status=GD_LJ204_UNSUPPORTED;result->exit_top=result->entry_top;}
 else{
  /* cpcall creates the protected native frame before allocating its closure.
   * No managed frame exists between a Lua error and this catcher. */
  status=c->api.cpcall(c->state,fixture?fixture_thunk:publish_thunk,NULL);
  result->lua_status=status;
  if(status!=0){
   result->flags|=GD_FLAG_UNWIND;
   if(result->bridge_status==GD_LJ204_OK)result->bridge_status=GD_LJ204_LUA_ERROR;
  }
  result->exit_top=c->api.gettop(c->state);
  if(status!=0&&result->exit_top>result->entry_top)result->flags|=GD_FLAG_ERROR_OBJECT;
 }
 /* OWN error cleanup, not a claim about original native C++ EH cleanup. */
 clear_scratch(c);c->callback_depth=0;c->gc_depth=0;active_raw_context=prior;
 result->published_mask=c->published;
 AcquireSRWLockExclusive(&contexts_lock);c->result=NULL;c->active=0;ReleaseSRWLockExclusive(&contexts_lock);
 return (int32_t)result->bridge_status;
}
int32_t __cdecl gd_lj204_publish_prefix(uint64_t token,gd_lj204_result *result){return run(token,0,result);}
int32_t __cdecl gd_lj204_execute_fixture(uint64_t token,uint32_t fixture,gd_lj204_result *result)
{
 if(fixture==0){if(result){init_result(result,token,fixture);result->bridge_status=GD_LJ204_INVALID;}return GD_LJ204_INVALID;}
 return run(token,fixture,result);
}

static void init_gl_result(gd_lj204_luaglue_result *r,uint64_t token)
{
 uint32_t i;memset(r,0,sizeof(*r));init_result(&r->call,token,0);
 r->struct_bytes=(uint32_t)sizeof(*r);r->current_record=-1;r->native_return_byte=-1;
 for(i=0;i<11;++i){
  gd_lj204_luaglue_record_result *p=&r->records[i];
  p->prepared_length=-1;p->load_status=-1;p->pcall_status=-1;
  p->top_before_load=-1;p->top_after_load=-1;p->top_after_pcall=-1;p->top_after_publish=-1;
  p->table_field_type=-1;p->global_type=-1;p->global_alias_equal=-1;p->snapshot_offset=UINT32_MAX;
 }
}
static void gl_stage(gd_context *c,lua_State *L,uint32_t phase,uint32_t at)
{
 c->gl_result->phase=phase;stage(c,L,at);
 if(c->gl_result->current_record>=0&&c->gl_result->current_record<11){
  gd_lj204_luaglue_record_result *r=&c->gl_result->records[c->gl_result->current_record];
  r->phase=phase;r->stage_rva=at;
 }
}
static void clear_gl_prepared(gd_context *c)
{
 if(c->gl_prepared&&!HeapFree(GetProcessHeap(),0,c->gl_prepared)&&c->completion_result){
  c->gl_release_failed=1;
  fail(c,c->state,GD_LJ204_MEMORY,"OWN prepared buffer release failed");return;
 }
 c->gl_prepared=NULL;c->gl_capacity=0;c->gl_length=0;
}
static void clear_gl_storage(gd_context *c)
{
 uint32_t i;clear_gl_prepared(c);
 for(i=0;i<11;++i){
  gd_gl_owned_record *r=&c->gl_records[i];
  if(r->name)HeapFree(GetProcessHeap(),0,r->name);
  if(r->chunk)HeapFree(GetProcessHeap(),0,r->chunk);
  if(r->source)HeapFree(GetProcessHeap(),0,r->source);
  memset(r,0,sizeof(*r));
 }
 if(c->gl_snapshot)HeapFree(GetProcessHeap(),0,c->gl_snapshot);
 c->gl_snapshot=NULL;
}
static unsigned char *gl_copy(gd_context *c,lua_State *L,const unsigned char *source,size_t bytes)
{
 unsigned char *copy=(unsigned char *)HeapAlloc(GetProcessHeap(),0,bytes);
 if(!copy){fail(c,L,GD_LJ204_MEMORY,"OWN LuaGlue record allocation failed");return NULL;}
 memcpy(copy,source,bytes);return copy;
}
static void gl_construct_records(gd_context *c,lua_State *L)
{
 uint32_t i;
 /* All11 records are materialized before executing any source. Heap storage
  * is an OWN allocator adaptation; failed allocations are not original C++ EH. */
 for(i=0;i<11;++i){
  const gd_gl_embedded_record *input=&gd_gl_inputs[i];gd_gl_owned_record *r=&c->gl_records[i];
  c->gl_result->current_record=(int32_t)i;gl_stage(c,L,GD_LJ204_GL_RECORDS,0);
  r->name_length=input->name_length;r->chunk_length=input->chunk_length;r->source_length=input->source_length;
  r->publish=(unsigned char)input->publish;
  r->name=gl_copy(c,L,input->name,(size_t)input->name_length+1);
  if(c->completion_result){c->gl_record_live[i]|=4u;++c->completion_result->live_record_allocations;}
  r->chunk=gl_copy(c,L,input->chunk,(size_t)input->chunk_length+1);
  if(c->completion_result){c->gl_record_live[i]|=2u;++c->completion_result->live_record_allocations;}
  r->source=gl_copy(c,L,input->source,(size_t)input->source_length+1);
  if(c->completion_result){c->gl_record_live[i]|=1u;++c->completion_result->live_record_allocations;++c->completion_result->records_constructed;}
 }
}
static void gl_append_byte(gd_context *c,lua_State *L,unsigned char byte)
{
 /*255CE0 normal byte append. Fixed reviewed inputs bound lengths to2091;
  * this is not a contract for overflow, invalid objects or reentry. */
 if(c->gl_capacity<c->gl_length+2){
  unsigned char *next;size_t capacity=(c->gl_length+2)*2;
  if(capacity<8)capacity=8;c->gl_capacity=capacity; /* Store capacity BEFORE allocation. */
  next=(unsigned char *)HeapAlloc(GetProcessHeap(),0,capacity);
  if(!next){fail(c,L,GD_LJ204_MEMORY,"OWN LuaGlue append allocation failed");return;}
  if(c->gl_length!=0)memcpy(next,c->gl_prepared,c->gl_length+1);
  if(c->gl_prepared)HeapFree(GetProcessHeap(),0,c->gl_prepared);
  c->gl_prepared=next;
 }
 c->gl_prepared[c->gl_length]=byte;++c->gl_length;c->gl_prepared[c->gl_length]=0;
}
static void gl_prepare(gd_context *c,lua_State *L,uint32_t index)
{
 const gd_gl_owned_record *input=&c->gl_records[index];uint32_t at;
 gd_lj204_luaglue_result *out=c->gl_result;gd_lj204_luaglue_record_result *r=&out->records[index];
 /*255F70: zero pointer/capacity/length, allocate declared source length,
  * then32-bit index; skip byte09 only. No decoding or whitespace normalization. */
 c->gl_prepared=NULL;c->gl_capacity=0;c->gl_length=0;
 if(input->source_length!=0){
  c->gl_prepared=(unsigned char *)HeapAlloc(GetProcessHeap(),0,input->source_length);
  if(!c->gl_prepared){fail(c,L,GD_LJ204_MEMORY,"OWN LuaGlue prepare allocation failed");return;}
  c->gl_capacity=input->source_length;
 }
 for(at=0;at<input->source_length;++at)if(input->source[at]!=9)gl_append_byte(c,L,input->source[at]);
 if(c->gl_length>8192u-out->snapshot_bytes){fail(c,L,GD_LJ204_UNSUPPORTED,"OWN LuaGlue snapshot capacity exceeded");return;}
 r->prepared_length=(int32_t)c->gl_length;r->snapshot_offset=out->snapshot_bytes;r->snapshot_bytes=(uint32_t)c->gl_length;
 if(c->gl_length)memcpy(c->gl_snapshot+out->snapshot_bytes,c->gl_prepared,c->gl_length);
 out->snapshot_bytes+=(uint32_t)c->gl_length;r->prepared=1;++out->prepared_count;
}
static void gl_observe(gd_context *c,lua_State *L)
{
 uint32_t i;int top=c->api.gettop(L);gd_lj204_luaglue_result *out=c->gl_result;
 /* OWN readback only after all11 native normal record paths. Raw lookups
  * bypass metatables, never invoke a defined script body, and restore top.
  * On a prior failure, all these observation fields remain unobserved=-1. */
 out->current_record=-1;out->phase=GD_LJ204_GL_OBSERVE;stage(c,L,0);
 for(i=0;i<11;++i){
  gd_lj204_luaglue_record_result *r=&out->records[i];const gd_gl_owned_record *record=&c->gl_records[i];
  c->api.pushstring(L,"__luaglue");c->api.rawget(L,LUA_GLOBALSINDEX);
  if(c->api.type(L,-1)!=LUA_TTABLE){
   c->api.settop(L,top);out->call.bridge_status=GD_LJ204_UNSUPPORTED;return;
  }
  c->api.pushstring(L,(const char *)record->name);c->api.rawget(L,-2);
  r->table_field_type=c->api.type(L,-1);++out->table_observed_count;
  if(r->published){
   c->api.pushstring(L,(const char *)record->name);c->api.rawget(L,LUA_GLOBALSINDEX);
   r->global_type=c->api.type(L,-1);r->global_alias_equal=c->api.rawequal(L,-1,-2);++out->globals_observed_count;
  }
  c->api.settop(L,top);
 }
}
enum gd_gl_progress { GD_GL_INCOMPLETE=0,GD_GL_RECORDS_READY=1,GD_GL_PCALL_FAILED=2 };
static int luaglue_body(gd_context *c,lua_State *L)
{
 gd_lj204_luaglue_result *out=c->gl_result;uint32_t i;
 gl_stage(c,L,GD_LJ204_GL_VERSION,0x256905);c->api.createtable(L,0,0);out->version_publication_mask|=1u;
 gl_stage(c,L,GD_LJ204_GL_VERSION,0x256915);c->api.pushstring(L,(const char *)gd_gl_version);
 gl_stage(c,L,GD_LJ204_GL_VERSION,0x25692a);c->api.setfield(L,-2,"version");out->version_publication_mask|=2u;
 gl_stage(c,L,GD_LJ204_GL_VERSION,0x25693a);c->api.setfield(L,LUA_GLOBALSINDEX,"__luaglue");out->version_publication_mask|=4u;
 gl_stage(c,L,GD_LJ204_GL_VERSION,0x25694a);c->api.pushstring(L,(const char *)gd_gl_version);
 gl_stage(c,L,GD_LJ204_GL_VERSION,0x25695a);c->api.setfield(L,LUA_GLOBALSINDEX,"__luaglue_version");out->version_publication_mask|=8u;
 gl_construct_records(c,L);
 for(i=0;i<11;++i){
  gd_lj204_luaglue_record_result *r=&out->records[i];gd_gl_owned_record *record=&c->gl_records[i];
  out->current_record=(int32_t)i;gl_stage(c,L,GD_LJ204_GL_PREPARE,0x257595);gl_prepare(c,L,i);
  gl_stage(c,L,GD_LJ204_GL_LOAD,0x2575ac);r->top_before_load=c->api.gettop(L);
  r->load_status=c->api.loadbuffer(L,(const char *)c->gl_prepared,c->gl_length,(const char *)record->chunk);
  r->top_after_load=c->api.gettop(L);
  if(r->load_status!=0){
   /* Actual status retained.255BC0 error-string handling is not supplied.
    * OWN rethrow preserves the actual error object outside cpcall's frame. */
   gl_stage(c,L,GD_LJ204_GL_LOAD,0x25772d);out->call.bridge_status=GD_LJ204_UNSUPPORTED;
   return c->api.error(L);
  }
  ++out->loaded_count;
  gl_stage(c,L,GD_LJ204_GL_PCALL,0x2575c5);r->pcall_status=c->api.pcall(L,0,0,0);
  r->top_after_pcall=c->api.gettop(L);
  if(r->pcall_status!=0){
   if(c->completion_result){
    /* A bridge-generated Unsupported/host/memory error is NOT evidence of
     * the original callback's behavior. Keep that exact stop, no native0. */
    if(c->result->bridge_status!=GD_LJ204_OK)return c->api.error(L);
    return GD_GL_PCALL_FAILED; /* Known Lua error retained for257774. */
   }
   if(out->call.bridge_status==GD_LJ204_OK)out->call.bridge_status=GD_LJ204_LUA_ERROR;
   /* Retain the actual pcall status separately from this OWN outer rethrow.
    * No invented error-stack pop, rollback, native return or destructor proof. */
   return c->api.error(L);
  }
  ++out->executed_count;
  if(record->publish!=0){ /* Re-read the live owned byte after pcall. */
   gl_stage(c,L,GD_LJ204_GL_PUBLISH,0x2575e2);
   c->api.getfield(L,LUA_GLOBALSINDEX,"__luaglue"); /* Evidenced258DA0 wrapper. */
   gl_stage(c,L,GD_LJ204_GL_PUBLISH,0x2575f1);c->api.getfield(L,-1,(const char *)record->name);
   gl_stage(c,L,GD_LJ204_GL_PUBLISH,0x2575fd);c->api.setfield(L,LUA_GLOBALSINDEX,(const char *)record->name);
   r->published=1;++out->published_count;
   gl_stage(c,L,GD_LJ204_GL_PUBLISH,0x25760b);c->api.settop(L,-2);r->top_after_publish=c->api.gettop(L);
  }
  clear_gl_prepared(c);
 }
 gl_observe(c,L);
 if(out->phase==GD_LJ204_GL_OBSERVE&&out->call.bridge_status!=GD_LJ204_OK)return GD_GL_INCOMPLETE;
 out->current_record=11;gl_stage(c,L,GD_LJ204_GL_REGISTRY_BOUNDARY,0x25762b);
 c->luaglue_completed=1; /* Native producer qualification, never managed receipt data. */
 return GD_GL_RECORDS_READY;
}
static int __cdecl luaglue_thunk(lua_State *L)
{
 gd_context *c=active_raw_context;int progress;
 c->api.settop(L,0); /* Remove only the OWN cpcall argument. */
 progress=luaglue_body(c,L);
 if(progress==GD_GL_RECORDS_READY)c->result->bridge_status=GD_LJ204_UNSUPPORTED;
 return 0; /* Old prefix still stops at25762B and releases its own storage. */
}
int32_t __cdecl gd_lj204_initialize_luaglue(uint64_t token,gd_lj204_luaglue_result *out,
 unsigned char *snapshot_out,uint32_t capacity)
{
 gd_context *c,*prior;int32_t admission=GD_LJ204_OK;int status;
 if(!out)return GD_LJ204_INVALID;init_gl_result(out,token);
 if(!snapshot_out||capacity!=8192u){out->call.bridge_status=GD_LJ204_INVALID;return GD_LJ204_INVALID;}
 AcquireSRWLockExclusive(&contexts_lock);c=find_token(token);
 if(!c||c->closing)admission=GD_LJ204_CLOSED;
 else if(c->active||active_raw_context)admission=GD_LJ204_BUSY;
 else if(c->owner_thread!=GetCurrentThreadId())admission=GD_LJ204_WRONG_THREAD;
 else if(c->published!=127u||c->luaglue_attempted)admission=GD_LJ204_UNSUPPORTED;
 else{c->active=1;c->result=&out->call;c->gl_result=out;}
 ReleaseSRWLockExclusive(&contexts_lock);
 if(admission!=GD_LJ204_OK){out->call.bridge_status=(uint32_t)admission;return admission;}
 prior=active_raw_context;active_raw_context=c;
 out->call.entry_top=c->api.gettop(c->state);
 if(out->call.entry_top!=0){out->call.bridge_status=GD_LJ204_UNSUPPORTED;out->call.exit_top=out->call.entry_top;}
 else{
  c->gl_snapshot=(unsigned char *)HeapAlloc(GetProcessHeap(),0,8192);
  if(!c->gl_snapshot){out->call.bridge_status=GD_LJ204_MEMORY;out->call.exit_top=out->call.entry_top;}
  else{
   c->luaglue_attempted=1;
   /* Caller0F2ECF captured and checked this context's VM exactly once. Never
    * re-read/rebind service.Lua00 here or between the11 records. */
   status=c->api.cpcall(c->state,luaglue_thunk,NULL);out->call.lua_status=status;
   if(status!=0){out->call.flags|=GD_FLAG_UNWIND;
    if(out->call.bridge_status==GD_LJ204_OK)out->call.bridge_status=GD_LJ204_LUA_ERROR;}
   out->call.exit_top=c->api.gettop(c->state);
   if(status!=0&&out->call.exit_top>out->call.entry_top)out->call.flags|=GD_FLAG_ERROR_OBJECT;
  }
 }
 /* Only returned protected execution can expose the captured bytes. No
  * prefilled hashes or assumed snapshot length. Partial results stay exact. */
 if(out->snapshot_bytes!=0)memcpy(snapshot_out,c->gl_snapshot,out->snapshot_bytes);
 clear_gl_storage(c);clear_scratch(c);c->callback_depth=0;c->gc_depth=0;active_raw_context=prior;
 out->call.published_mask=c->published;
 AcquireSRWLockExclusive(&contexts_lock);c->result=NULL;c->gl_result=NULL;c->active=0;ReleaseSRWLockExclusive(&contexts_lock);
 return (int32_t)out->call.bridge_status;
}
static int gc_refuse(gd_context *c,lua_State *L,uint32_t at,const char *message)
{
 stage(c,L,at);++c->gc_audit.unsupported_callbacks;c->gc_audit.last_failure_rva=at;
 c->gc_audit.last_callback_return=-1;c->gc_depth=0;
 return fail(c,L,GD_LJ204_UNSUPPORTED,message);
}
static gd_gc_own_userdata *gc_entry(gd_context *c,void *userdata)
{
 uint32_t i;for(i=0;i<8;++i)if(c->gc_userdata[i].used&&c->gc_userdata[i].userdata==userdata)return &c->gc_userdata[i];
 return NULL;
}
static gd_gc_own_userdata *gc_enter(gd_context *c,lua_State *L,uint32_t kind)
{
 void *userdata;gd_gc_own_userdata *entry;uint32_t at=kind==1?0x256159u:0x2561a7u;
 /* Dedicated native GC entry, not print/panic's closing/scratch contract.
  * LuaJIT invokes it under native protection both in GC and during lua_close.
  * No managed call, arbitrary destructor, or foreign allocation is admitted. */
 stage(c,L,at);++c->result->callback_calls;c->gc_audit.last_callback_return=-1;
 if(kind==1)++c->gc_audit.list_entered;else ++c->gc_audit.object_entered;
 if(c->closing)++c->gc_audit.close_callbacks;
 if(c->gc_depth){gc_refuse(c,L,at,"OWN recursive GC callback unsupported");return NULL;}
 c->gc_depth=1;userdata=c->api.touserdata(L,-1);
 entry=gc_entry(c,userdata);
 if(c->api.type(L,-1)!=LUA_TUSERDATA||!entry||entry->kind!=kind||!entry->admitted||entry->finalizer_seen||entry->finalized){
  if(entry&&entry->script_index){++c->script_audit.unsupported_gc;c->script_audit.last_failure_rva=kind==1?0x256162u:0x2561adu;c->script_audit.last_callback_return=-1;}
  gc_refuse(c,L,kind==1?0x256162u:0x2561adu,"Unknown,foreign,unadmitted or consumed OWN userdata");return NULL;
 }
 /* LuaJIT gc_finalize unchains/makes U white BEFORE calling __gc; a throwing
  * finalizer does not preserve U lifetime. Consume provenance before any body
  * read/throw, independently from the normal-completion bit and audit counts. */
 entry->finalizer_seen=1;
 return entry;
}
static int __cdecl native_gc_list(lua_State *L)
{
 gd_context *c=callback_context(L);gd_gc_own_userdata *entry;void *node,*buffer,*check;
 if(!c)return 0;entry=gc_enter(c,L,1);if(!entry)return 0;
 stage(c,L,0x256162);memcpy(&node,(unsigned char *)entry->userdata+0x18,sizeof(node));
 if(node!=NULL)return gc_refuse(c,L,0x256170,"Nonempty node type/vslot0 remains unproved");
 /* Native reads the buffer only AFTER the complete chain. Empty is an explicit
  * OWN fixture value, never a guessed initialization of an unknown payload. */
 stage(c,L,0x25618c);memcpy(&buffer,entry->userdata,sizeof(buffer));
 stage(c,L,0x25618f);
 if(buffer!=entry->initial_payload||(buffer!=NULL&&(!entry->buffer_live||buffer!=entry->buffer)))
  return gc_refuse(c,L,0x25618f,"Foreign,changed or already freed buffer rejected");
 if(buffer!=NULL){
  ++c->gc_audit.own_buffer_free_calls;
  if(!HeapFree(GetProcessHeap(),0,buffer)){
   c->gc_audit.last_failure_rva=0x25618f;c->gc_depth=0;
   return fail(c,L,GD_LJ204_MEMORY,"OWN buffer HeapFree failed; resource remains owned");
  }
  ++c->gc_audit.own_buffer_free_successes;--c->gc_audit.live_own_buffers;entry->buffer_live=0;
 }
 /* Do not zero userdata+00 or+18. The retained pointer value is not reused. */
 memcpy(&check,entry->userdata,sizeof(check));
 if(check==entry->initial_payload){memcpy(&check,(unsigned char *)entry->userdata+0x18,sizeof(check));
  if(check==entry->initial_head)++c->gc_audit.fields_unchanged_checks;}
 entry->finalized=1;++c->gc_audit.list_completed;c->gc_audit.last_callback_return=0;c->gc_depth=0;
 stage(c,L,0x256194);return 0;
}
static int __cdecl native_gc_object(lua_State *L)
{
 gd_context *c=callback_context(L);gd_gc_own_userdata *entry;void *object,*check;
 if(!c)return 0;entry=gc_enter(c,L,2);if(!entry)return 0;
 stage(c,L,0x2561ad);memcpy(&object,entry->userdata,sizeof(object));
 if(object!=NULL){if(!script_gc_object(c,L,entry,object))return 0;}
 memcpy(&check,entry->userdata,sizeof(check));if(check==entry->initial_payload)++c->gc_audit.fields_unchanged_checks;
 entry->finalized=1;++c->gc_audit.object_completed;c->gc_audit.last_callback_return=0;c->gc_depth=0;
 stage(c,L,0x2561bf);return 0;
}
static int gc_fixture(gd_context *c,lua_State *L,uint32_t fixture)
{
 uint32_t i,kind=(fixture==GD_LJ204_GC_OBJECT_COLLECT||fixture==GD_LJ204_GC_OBJECT_CLOSE||fixture==GD_LJ204_GC_NONNULL_OBJECT)?2u:1u;
 int registry,for_close=fixture==GD_LJ204_GC_LIST_CLOSE||fixture==GD_LJ204_GC_OBJECT_CLOSE||fixture==GD_LJ204_GC_UNKNOWN_CLOSE;
 int unknown=fixture==GD_LJ204_GC_UNKNOWN_CLOSE;gd_gc_own_userdata *entry=NULL;void *userdata,*payload=NULL,*head=NULL;
 lua_CFunction expected=kind==1?native_gc_list:native_gc_object;
 c->gc_fixture_mask|=1u<<(fixture-GD_LJ204_GC_LIST_COLLECT);
 registry=resolve(c,L,0,GD_LJ204_READ_REGISTRY);c->api.rawgeti(L,registry,kind==1?-3:-4);
 if(c->api.type(L,-1)!=LUA_TTABLE)return fail(c,L,GD_LJ204_UNSUPPORTED,"OWN fixture registry metatable missing");
 c->api.pushstring(L,own_gc_key);c->api.rawget(L,-2);
 if(c->api.tocfunction(L,-1)!=expected)return fail(c,L,GD_LJ204_UNSUPPORTED,"OWN fixture GC function identity differs");
 c->api.settop(L,-2); /* Keep verified metatable. */
 userdata=c->api.newuserdata(L,kind==1?0x20u:sizeof(void *));++c->gc_audit.fixture_userdata_created;
 entry=gc_entry(c,userdata);
 if(unknown){
  /* A new allocation can reuse an old address. Unknown U never inherits the
   * old allocation's provenance; retain any buffer ownership for quarantine. */
  if(entry)entry->admitted=0;
  entry=NULL;
 }
 if(!unknown){
  /* Lua can reuse an address only through a newly returned userdata allocation.
   * Replace only consumed/unadmitted records without a live buffer. An old U
   * is never dereferenced, and a failed callback is not a normal completion. */
  if(entry&&((entry->admitted&&!entry->finalizer_seen)||entry->buffer_live||script_entry_has_resources(c,entry))){
   entry->admitted=0;return fail(c,L,GD_LJ204_UNSUPPORTED,"OWN userdata address has unresolved prior ownership");
  }
  if(!entry)for(i=0;i<8;++i)if(!c->gc_userdata[i].used||((c->gc_userdata[i].finalizer_seen||!c->gc_userdata[i].admitted)&&!c->gc_userdata[i].buffer_live&&!script_entry_has_resources(c,&c->gc_userdata[i]))){entry=&c->gc_userdata[i];break;}
  if(!entry)return fail(c,L,GD_LJ204_UNSUPPORTED,"OWN finite userdata fixture slots exhausted");
  memset(entry,0,sizeof(*entry));entry->userdata=userdata;entry->kind=kind;entry->used=1;
  if(fixture==GD_LJ204_GC_LIST_COLLECT||fixture==GD_LJ204_GC_LIST_CLOSE){
   payload=HeapAlloc(GetProcessHeap(),0,16);
   if(!payload)return fail(c,L,GD_LJ204_MEMORY,"OWN fixture buffer allocation failed");
   entry->buffer=payload;entry->buffer_live=1;++c->gc_audit.own_buffer_allocations;++c->gc_audit.live_own_buffers;
  }else if(fixture==GD_LJ204_GC_FOREIGN_BUFFER||fixture==GD_LJ204_GC_NONNULL_OBJECT)payload=(void *)(uintptr_t)1;
  if(fixture==GD_LJ204_GC_NONEMPTY_LIST)head=(void *)(uintptr_t)1;
  entry->initial_payload=payload;entry->initial_head=head;
  memcpy(userdata,&payload,sizeof(payload));if(kind==1)memcpy((unsigned char *)userdata+0x18,&head,sizeof(head));
 }
 /* Unknown fixture payload bytes are deliberately not read or defaulted. */
 c->api.pushvalue(L,-2);
 if(c->api.setmetatable(L,-2)!=1)return fail(c,L,GD_LJ204_UNSUPPORTED,"OWN userdata metatable was not installed");
 /* Allocation/admission failures cannot leave an eligible partial record.
  * Any already-owned buffer stays live and prevents CompleteClose release. */
 if(entry)entry->admitted=1;
 if(for_close){
  /* OWN positive fixture keys hold U reachable until lua_close finalizes all;
   * no ordering is promised when several userdata are finalized. */
  c->api.pushvalue(L,-1);c->api.rawseti(L,registry,(int)(0x47440000u+fixture));
 }
 c->api.settop(L,0);
 if(!for_close)c->api.gc(L,LUA_GCCOLLECT,0); /* Actual Lua GC, never direct fake callback invocation. */
 return 0;
}
static void init_registry_result(gd_lj204_registry_result *r,uint64_t token)
{
 uint32_t i;memset(r,0,sizeof(*r));init_result(&r->call,token,0);r->struct_bytes=(uint32_t)sizeof(*r);
 r->luaglue_alias=-1;r->native_return_byte=-1;for(i=0;i<4;++i)r->registry_types[i]=-1;
}
static void registry_store(gd_context *c,lua_State *L,uint32_t read_at,uint32_t call_at,int key,uint32_t bit)
{
 int registry=resolve(c,L,read_at,GD_LJ204_READ_REGISTRY);
 stage(c,L,call_at);c->api.rawseti(L,registry,key);c->registry_published|=bit;
}
static int registry_body(gd_context *c,lua_State *L)
{
 gd_lj204_registry_result *out=c->registry_result;uint32_t i;int top;
 out->phase=1;
 stage(c,L,0x257635);c->api.getfield(L,LUA_GLOBALSINDEX,"__luaglue");
 registry_store(c,L,0x25763f,0x257648,-1,1u);
 stage(c,L,0x257656);c->api.createtable(L,0,0);
 registry_store(c,L,0x257662,0x25766b,-2,2u);
 stage(c,L,0x257679);c->api.createtable(L,0,0);
 stage(c,L,0x25768c);c->api.pushcclosure(L,native_gc_list,0);
 stage(c,L,0x2576a1);c->api.setfield(L,-2,own_gc_key);
 registry_store(c,L,0x2576ad,0x2576b6,-3,4u);
 stage(c,L,0x2576c4);c->api.createtable(L,0,0);
 stage(c,L,0x2576d7);c->api.pushcclosure(L,native_gc_object,0);
 stage(c,L,0x2576ec);c->api.setfield(L,-2,own_gc_key);
 registry_store(c,L,0x2576f8,0x257701,-4,8u);
 /* Explicit OWN raw observations, no extra live-index reads or script calls. */
 out->phase=2;stage(c,L,0);top=c->api.gettop(L);
 for(i=0;i<4;++i){
  c->api.rawgeti(L,LUA_REGISTRYINDEX,-1-(int)i);out->registry_types[i]=c->api.type(L,-1);out->observed_mask|=1u<<i;
  if(i==0){c->api.pushstring(L,"__luaglue");c->api.rawget(L,LUA_GLOBALSINDEX);out->luaglue_alias=c->api.rawequal(L,-1,-2);}
  if(i>=2&&out->registry_types[i]==LUA_TTABLE){
   c->api.pushstring(L,own_gc_key);c->api.rawget(L,-2);
   if(c->api.tocfunction(L,-1)==(i==2?native_gc_list:native_gc_object))out->gc_closure_match_mask|=1u<<(i-2);
  }
  c->api.settop(L,top);
 }
 out->phase=3;stage(c,L,0x257707);return 1;
}
static int __cdecl registry_thunk(lua_State *L)
{
 gd_context *c=active_raw_context;c->api.settop(L,0);
 if(registry_body(c,L))c->result->bridge_status=GD_LJ204_UNSUPPORTED;
 return 0;
}
int32_t __cdecl gd_lj204_publish_luaglue_registry(uint64_t token,gd_lj204_registry_result *out)
{
 gd_context *c,*prior;int32_t admission=GD_LJ204_OK;int status;
 if(!out)return GD_LJ204_INVALID;init_registry_result(out,token);
 AcquireSRWLockExclusive(&contexts_lock);c=find_token(token);
 if(!c||c->closing)admission=GD_LJ204_CLOSED;
 else if(c->active||active_raw_context)admission=GD_LJ204_BUSY;
 else if(c->owner_thread!=GetCurrentThreadId())admission=GD_LJ204_WRONG_THREAD;
 else if(!c->luaglue_completed||c->registry_attempted)admission=GD_LJ204_UNSUPPORTED;
 else{c->active=1;c->result=&out->call;c->registry_result=out;}
 ReleaseSRWLockExclusive(&contexts_lock);
 if(admission!=GD_LJ204_OK){out->call.bridge_status=(uint32_t)admission;return admission;}
 prior=active_raw_context;active_raw_context=c;out->call.entry_top=c->api.gettop(c->state);
 if(out->call.entry_top!=0){out->call.bridge_status=GD_LJ204_UNSUPPORTED;out->call.exit_top=out->call.entry_top;}
 else{
  c->registry_attempted=1;status=c->api.cpcall(c->state,registry_thunk,NULL);out->call.lua_status=status;
  if(status!=0){out->call.flags|=GD_FLAG_UNWIND;if(out->call.bridge_status==GD_LJ204_OK)out->call.bridge_status=GD_LJ204_LUA_ERROR;}
  out->call.exit_top=c->api.gettop(c->state);
  if(status!=0&&out->call.exit_top>out->call.entry_top)out->call.flags|=GD_FLAG_ERROR_OBJECT;
 }
 out->registry_published_mask=c->registry_published;out->call.published_mask=c->published;
 clear_scratch(c);c->callback_depth=0;c->gc_depth=0;active_raw_context=prior;
 AcquireSRWLockExclusive(&contexts_lock);c->result=NULL;c->registry_result=NULL;c->active=0;ReleaseSRWLockExclusive(&contexts_lock);
 return (int32_t)out->call.bridge_status;
}
static void init_completion_result(gd_lj204_completion_result *out,uint64_t token)
{
 uint32_t i;memset(out,0,sizeof(*out));init_gl_result(&out->glue,token);
 out->struct_bytes=(uint32_t)sizeof(*out);out->flags_known=-1;out->log_returned=-1;
 out->cleanup_current_record=-1;out->records_live_at_flag=-1;out->luaglue_alias=-1;
 for(i=0;i<4;++i)out->registry_types[i]=-1;
}
static void *completion_record_pointer(gd_gl_owned_record *record,uint32_t component)
{
 return component==0?record->source:(component==1?record->chunk:record->name);
}
static void completion_destroy_record(gd_context *c,lua_State *L,uint32_t index)
{
 gd_lj204_completion_result *out=c->completion_result;uint32_t component,bit;void *pointer;
 /* OWN typed, private destructor target replaces original loader/CFG target
  * admission. No original guard slot, allocator ABI or arbitrary vtable runs.
  * Keep original pointer values until all records are retired; a separate live
  * ownership bitmap prevents duplicate frees without claiming native zeroing. */
 for(component=0;component<3;++component){
  bit=1u<<component;pointer=completion_record_pointer(&c->gl_records[index],component);
  stage(c,L,component==0?0x2577cdu:(component==1?0x2577d6u:0x2577e3u));
  if(!(c->gl_record_live[index]&bit)||!pointer)
   {fail(c,L,GD_LJ204_UNSUPPORTED,"OWN normal record allocation identity is unavailable");return;}
  ++out->normal_free_attempts;
  if(!HeapFree(GetProcessHeap(),0,pointer)){c->gl_release_failed=1;fail(c,L,GD_LJ204_MEMORY,"OWN normal record release failed");return;}
  c->gl_record_live[index]&=~bit;--out->live_record_allocations;++out->normal_free_successes;
  out->cleanup_trace[out->cleanup_trace_count++]=index*3u+component;
 }
 ++out->normal_records_completed;
}
static void completion_normal_cleanup(gd_context *c,lua_State *L)
{
 gd_lj204_completion_result *out=c->completion_result;uint32_t remaining=11;
 out->phase=GD_LJ204_COMPLETE_CLEANUP;stage(c,L,0x257795);
 if(out->records_constructed!=11){fail(c,L,GD_LJ204_UNSUPPORTED,"OWN complete record array is unavailable");return;}
 out->normal_cleanup_entered=1;
 while(remaining!=0){
  --remaining;out->cleanup_current_record=(int32_t)remaining;
  /* The evidenced iterator uses index10..0 and validates its callback first.
   * This direct typed OWN target is not the original runtime GuardCF slot. */
  completion_destroy_record(c,L,remaining);
 }
}
static void completion_own_cleanup(gd_context *c)
{
 gd_lj204_completion_result *out=c->completion_result;uint32_t i,component,bit;void *pointer;int ok=1;
 /* Post-protection OWN cleanup is never credited as original normal cleanup.
  * Only actually live ledger entries may be released; failures preserve their
  * addresses/ownership and prevent CompleteClose from unloading the context. */
 if(c->gl_release_failed){out->glue.call.bridge_status=GD_LJ204_MEMORY;return;} /* No retry of a failed free. */
 for(i=11;i!=0;){--i;for(component=0;component<3;++component){
  bit=1u<<component;if(!(c->gl_record_live[i]&bit))continue;
  pointer=completion_record_pointer(&c->gl_records[i],component);++out->own_free_attempts;
  if(!pointer||!HeapFree(GetProcessHeap(),0,pointer)){ok=0;continue;}
  c->gl_record_live[i]&=~bit;--out->live_record_allocations;++out->own_free_successes;
 }}
 if(c->gl_prepared){if(HeapFree(GetProcessHeap(),0,c->gl_prepared)){
   c->gl_prepared=NULL;c->gl_capacity=0;c->gl_length=0;
  }else ok=0;}
 if(c->gl_snapshot){if(HeapFree(GetProcessHeap(),0,c->gl_snapshot))c->gl_snapshot=NULL;else ok=0;}
 if(ok&&out->live_record_allocations==0){
  memset(c->gl_records,0,sizeof(c->gl_records));c->gl_full_storage=0;
 }else{c->gl_release_failed=1;out->glue.call.bridge_status=GD_LJ204_MEMORY;}
}
static int __cdecl completion_thunk(lua_State *L)
{
 gd_context *c=active_raw_context;gd_lj204_completion_result *out=c->completion_result;
 int progress,answer;uint32_t flags=0,i;int32_t live_records=0;
 c->api.settop(L,0);out->phase=GD_LJ204_COMPLETE_GLUE;
 progress=luaglue_body(c,L);
 if(progress==GD_GL_PCALL_FAILED){
  /*257774..257779 frees the current prepared string, then BL=0. The actual
   * inner-pcall error is NOT popped. It must survive the OWN cpcall frame. */
  stage(c,L,0x257779);
  if(!c->gl_prepared)return fail(c,L,GD_LJ204_UNSUPPORTED,"OWN failed-pcall prepared allocation missing");
  if(!HeapFree(GetProcessHeap(),0,c->gl_prepared)){c->gl_release_failed=1;return fail(c,L,GD_LJ204_MEMORY,"OWN failed-pcall prepared release failed");}
  c->gl_prepared=NULL;c->gl_capacity=0;c->gl_length=0;++out->prepared_failure_releases;
  completion_normal_cleanup(c,L);
  out->phase=GD_LJ204_COMPLETE_RETURNED;stage(c,L,0x25779a);out->glue.native_return_byte=0;
  /* OWN transport AFTER the reconstructed normal0 path. Without rethrow,
   * cpcall's normal C-frame return would silently discard the real error. */
  out->glue.call.flags|=GD_FLAG_NORMAL_ZERO_TRANSFER;return c->api.error(L);
 }
 if(progress!=GD_GL_RECORDS_READY)return 0;
 out->phase=GD_LJ204_COMPLETE_REGISTRY;c->registry_attempted=1;
 if(!registry_body(c,L))return 0;
 out->phase=GD_LJ204_COMPLETE_FLAGS;stage(c,L,0x257707);
 for(i=0;i<11;++i)if(c->gl_record_live[i]==7u)++live_records;
 out->records_live_at_flag=live_records;
 ++out->glue.call.resolver_calls;
 answer=c->completion_flags(c->completion_cookie,0x257707,&flags);
 /* Managed POD callback has returned before any Lua error is raised. */
 if(answer!=0){out->flags_known=0;out->glue.call.host_status=answer;
  return fail(c,L,answer==2?GD_LJ204_HOST_ERROR:GD_LJ204_UNSUPPORTED,"OWN live LuaGlue flags DWORD unavailable");}
 out->flags_known=1;out->flags_dword=flags;
 if((flags&1u)!=0){out->phase=GD_LJ204_COMPLETE_LOG_BOUNDARY;stage(c,L,0x257724);
  return fail(c,L,GD_LJ204_UNSUPPORTED,"LuaGlue logging formatter/runtime options remain unproved");}
 completion_normal_cleanup(c,L);
 out->phase=GD_LJ204_COMPLETE_RETURNED;stage(c,L,0x25779a);out->glue.native_return_byte=1;
 return 0;
}
int32_t __cdecl gd_lj204_initialize_luaglue_completion(uint64_t token,
 gd_lj204_flags_reader flags_reader,void *flags_cookie,gd_lj204_completion_result *out,
 unsigned char *snapshot_out,uint32_t capacity)
{
 gd_context *c,*prior;gd_lj204_registry_result registry;int32_t admission=GD_LJ204_OK;int status;uint32_t i;
 if(!out)return GD_LJ204_INVALID;init_completion_result(out,token);init_registry_result(&registry,token);
 if(!flags_reader||!snapshot_out||capacity!=8192u){out->glue.call.bridge_status=GD_LJ204_INVALID;return GD_LJ204_INVALID;}
 AcquireSRWLockExclusive(&contexts_lock);c=find_token(token);
 if(!c||c->closing)admission=GD_LJ204_CLOSED;
 else if(c->active||active_raw_context)admission=GD_LJ204_BUSY;
 else if(c->owner_thread!=GetCurrentThreadId())admission=GD_LJ204_WRONG_THREAD;
 else if(c->published!=127u||c->luaglue_attempted||c->gl_full_storage)admission=GD_LJ204_UNSUPPORTED;
 else{
  c->active=1;c->result=&out->glue.call;c->gl_result=&out->glue;c->registry_result=&registry;
  c->completion_result=out;c->completion_flags=flags_reader;c->completion_cookie=flags_cookie;
 }
 ReleaseSRWLockExclusive(&contexts_lock);
 if(admission!=GD_LJ204_OK){out->glue.call.bridge_status=(uint32_t)admission;return admission;}
 prior=active_raw_context;active_raw_context=c;out->glue.call.entry_top=c->api.gettop(c->state);
 if(out->glue.call.entry_top!=0){out->glue.call.bridge_status=GD_LJ204_UNSUPPORTED;out->glue.call.exit_top=out->glue.call.entry_top;}
 else{
  c->gl_full_storage=1;c->gl_snapshot=(unsigned char *)HeapAlloc(GetProcessHeap(),0,8192);
  if(!c->gl_snapshot){out->glue.call.bridge_status=GD_LJ204_MEMORY;out->glue.call.exit_top=out->glue.call.entry_top;}
  else{
   c->luaglue_attempted=1;status=c->api.cpcall(c->state,completion_thunk,NULL);out->glue.call.lua_status=status;
   if(status!=0){out->glue.call.flags|=GD_FLAG_UNWIND;if(out->glue.call.bridge_status==GD_LJ204_OK)out->glue.call.bridge_status=GD_LJ204_LUA_ERROR;}
   out->glue.call.exit_top=c->api.gettop(c->state);
   if(status!=0&&out->glue.call.exit_top>out->glue.call.entry_top)out->glue.call.flags|=GD_FLAG_ERROR_OBJECT;
  }
 }
 if(out->glue.snapshot_bytes!=0)memcpy(snapshot_out,c->gl_snapshot,out->glue.snapshot_bytes);
 out->registry_published_mask=c->registry_published;out->registry_observed_mask=registry.observed_mask;
 for(i=0;i<4;++i)out->registry_types[i]=registry.registry_types[i];
 out->luaglue_alias=registry.luaglue_alias;out->gc_closure_match_mask=registry.gc_closure_match_mask;
 completion_own_cleanup(c);clear_scratch(c);c->callback_depth=0;c->gc_depth=0;active_raw_context=prior;
 out->glue.call.published_mask=c->published;
 AcquireSRWLockExclusive(&contexts_lock);
 c->result=NULL;c->gl_result=NULL;c->registry_result=NULL;c->completion_result=NULL;
 c->completion_flags=NULL;c->completion_cookie=NULL;c->active=0;
 ReleaseSRWLockExclusive(&contexts_lock);return (int32_t)out->glue.call.bridge_status;
}
int32_t __cdecl gd_lj204_read_gc_audit(uint64_t token,gd_lj204_gc_audit *audit)
{
 gd_context *c;int32_t status=GD_LJ204_OK;
 if(!audit)return GD_LJ204_INVALID;memset(audit,0,sizeof(*audit));audit->struct_bytes=(uint32_t)sizeof(*audit);
 audit->context_token=token;audit->last_callback_return=-1;
 AcquireSRWLockShared(&contexts_lock);c=find_token(token);
 if(!c)status=GD_LJ204_CLOSED;
 else if(c->active)status=GD_LJ204_BUSY;
 else if(GetCurrentThreadId()!=(c->closing?c->close_thread:c->owner_thread))status=GD_LJ204_WRONG_THREAD;
 else *audit=c->gc_audit;
 ReleaseSRWLockShared(&contexts_lock);return status;
}
/* Script uses a real local wrapper; it is not a second managed Lua owner. */
typedef struct gd_script_wrapper { lua_State *state;int mode,reference; } gd_script_wrapper;
static int __cdecl script_load_dispatch(lua_State *L);
static int __cdecl script_register_dispatch(lua_State *L);
static int __cdecl script_unregister_dispatch(lua_State *L);
typedef void *(__cdecl *gd_script_destroy)(gd_context *,lua_State *,gd_script_owned_node *);
typedef struct gd_script_vtable { gd_script_destroy destroy;uint32_t original_rva; } gd_script_vtable;
static void *__cdecl script_delete_node(gd_context *c,lua_State *L,gd_script_owned_node *owned);
static const gd_script_vtable script_vtables[3]={{script_delete_node,0x340180},{script_delete_node,0x3401e0},{script_delete_node,0x3401b0}};
static const gd_script_vtable script_base_vtable={NULL,0x340218};
static const uint32_t script_callback_tags[3]={0xef9f0,0xefa10,0xefa30};
static const char *const script_keys[3]={"Load","RegisterForUpdate","UnregisterForUpdate"};
static const lua_CFunction script_dispatchers[3]={script_load_dispatch,script_register_dispatch,script_unregister_dispatch};
static void script_stage(gd_context *c,lua_State *L,uint32_t at)
{
 stage(c,L,at);
 if(c->script_result&&c->script_binding_index>=0&&c->script_binding_index<3)
  c->script_result->bindings[c->script_binding_index].stage_rva=at;
}
static int script_fail(gd_context *c,lua_State *L,uint32_t at,uint32_t status,const char *message)
{
 script_stage(c,L,at);c->script_audit.last_failure_rva=at;c->script_audit.last_callback_return=-1;
 return fail(c,L,status,message);
}
static int64_t script_read(gd_context *c,lua_State *L,uint32_t at,uint32_t requested)
{
 int64_t value=0;int32_t answer;
 script_stage(c,L,at);++c->result->resolver_calls;
 if(!c->script_reader){script_fail(c,L,at,GD_LJ204_UNSUPPORTED,"OWN Script live reader missing");return 0;}
 answer=c->script_reader(c->script_cookie,at,requested,&value);
 if(answer!=0){c->result->host_status=answer;script_fail(c,L,at,answer==2?GD_LJ204_HOST_ERROR:GD_LJ204_UNSUPPORTED,"OWN Script live field unknown");return 0;}
 if((requested==GD_LJ204_SCRIPT_REGISTRY&&value!=LUA_REGISTRYINDEX)||
    (requested==GD_LJ204_SCRIPT_UPVALUE_BASE&&value!=LUA_GLOBALSINDEX)||
    ((requested==GD_LJ204_SCRIPT_VERSION||requested==GD_LJ204_SCRIPT_FLAGS)&&(value<0||value>UINT32_MAX))||
    (requested==GD_LJ204_SCRIPT_DESCRIPTOR&&(value<0||value>255))){
  script_fail(c,L,at,GD_LJ204_UNSUPPORTED,"OWN Script field outside its typed Lua51 profile");return 0;
 }
 return value;
}
static int script_entry_has_resources(gd_context *c,gd_gc_own_userdata *entry)
{
 gd_script_owned_node *node;
 if(!entry->script_index||entry->script_index>3)return 0;
 node=&c->script_nodes[entry->script_index-1];
 return node->epoch==entry->script_epoch&&(node->node_live||node->name_live);
}
static void *__cdecl script_delete_node(gd_context *c,lua_State *L,gd_script_owned_node *owned)
{
 gd_script_node_image *node=owned->node;void *identity=node;
 owned->destructor_seen=1;++c->script_audit.destructor_entered;
 if(c->closing)++c->script_audit.close_node_callbacks;
 script_stage(c,L,0xf13c4);node->vtable=&script_base_vtable;
 script_stage(c,L,0xf13c9);
 if(!owned->name_live||node->name!=owned->name){c->gc_depth=0;script_fail(c,L,0xf13c9,GD_LJ204_UNSUPPORTED,"OWN Script node name ownership differs");return NULL;}
 script_stage(c,L,0xf13cd);++c->script_audit.name_free_attempts;
 if(!HeapFree(GetProcessHeap(),0,owned->name)){c->gc_depth=0;script_fail(c,L,0xf13cd,GD_LJ204_MEMORY,"OWN Script node name free failed; retained");return NULL;}
 owned->name_live=0;--c->script_audit.live_node_names;++c->script_audit.name_free_successes;
 /*2561A0 passes flags=1. No recursive next destruction or pointer clearing. */
 script_stage(c,L,0xf13df);++c->script_audit.node_free_attempts;
 if(!HeapFree(GetProcessHeap(),0,node)){c->gc_depth=0;script_fail(c,L,0xf13df,GD_LJ204_MEMORY,"OWN Script node free failed; retained");return NULL;}
 owned->node_live=0;--c->script_audit.live_nodes;++c->script_audit.node_free_successes;
 ++c->script_audit.destructor_completed;c->script_audit.last_callback_return=0;
 /* A returned pointer is only an identity value; no node dereference follows. */
 return identity;
}
static int script_gc_object(gd_context *c,lua_State *L,gd_gc_own_userdata *entry,void *object)
{
 gd_script_owned_node *owned;void *returned;
 if(!entry->script_index||entry->script_index>3)return gc_refuse(c,L,0x2561b5,"Non-null object type/vslot0 remains unproved");
 owned=&c->script_nodes[entry->script_index-1];
 if(owned->epoch!=entry->script_epoch||owned->userdata!=entry->userdata||!owned->published||!owned->node_live||owned->destructor_seen||owned->node!=object||owned->kind>2){
  ++c->script_audit.unsupported_gc;c->script_audit.last_failure_rva=0x2561b5;
  return gc_refuse(c,L,0x2561b5,"OWN Script node identity/epoch is unavailable");
 }
 script_stage(c,L,0x2561b5);
 if(owned->node->vtable!=&script_vtables[owned->kind]){
  ++c->script_audit.unsupported_gc;c->script_audit.last_failure_rva=0x2561b5;
  return gc_refuse(c,L,0x2561b5,"OWN Script node vtable identity differs");
 }
 script_stage(c,L,0x2561bd);
 returned=script_vtables[owned->kind].destroy(c,L,owned);
 if(returned!=object){c->gc_depth=0;script_fail(c,L,0xf13e4,GD_LJ204_UNSUPPORTED,"OWN Script destructor identity differs");return 0;}
 return 1;
}
static gd_gc_own_userdata *script_track_userdata(gd_context *c,lua_State *L,void *userdata,uint32_t kind,uint32_t at)
{
 gd_gc_own_userdata *entry=gc_entry(c,userdata);uint32_t i;
 if(entry&&((entry->admitted&&!entry->finalizer_seen)||entry->buffer_live||script_entry_has_resources(c,entry))){
  entry->admitted=0;script_fail(c,L,at,GD_LJ204_UNSUPPORTED,"OWN Script userdata has unresolved prior ownership");return NULL;
 }
 if(!entry)for(i=0;i<8;++i)if(!c->gc_userdata[i].used||((c->gc_userdata[i].finalizer_seen||!c->gc_userdata[i].admitted)&&!c->gc_userdata[i].buffer_live&&!script_entry_has_resources(c,&c->gc_userdata[i]))){entry=&c->gc_userdata[i];break;}
 if(!entry){script_fail(c,L,at,GD_LJ204_UNSUPPORTED,"OWN finite userdata ledger exhausted");return NULL;}
 memset(entry,0,sizeof(*entry));entry->userdata=userdata;entry->kind=2;entry->used=1;
 entry->script_index=kind+1;entry->script_epoch=++c->script_epoch;
 c->script_nodes[kind].userdata=userdata;c->script_nodes[kind].epoch=entry->script_epoch;c->script_nodes[kind].kind=kind;
 return entry; /* Deliberately unadmitted until the real U[0] store. */
}
static unsigned char *script_temp_alloc(gd_context *c,lua_State *L,uint32_t slot,size_t bytes,uint32_t at)
{
 unsigned char *p;script_stage(c,L,at);
 if(slot>=4||c->script_temp[slot]){script_fail(c,L,at,GD_LJ204_UNSUPPORTED,"OWN Script temporary slot already occupied");return NULL;}
 p=(unsigned char *)HeapAlloc(GetProcessHeap(),0,bytes);
 if(!p){script_fail(c,L,at,GD_LJ204_MEMORY,"OWN Script temporary allocation failed");return NULL;}
 c->script_temp[slot]=p;++c->script_result->live_temp_allocations;return p;
}
static void script_temp_release(gd_context *c,lua_State *L,uint32_t slot,uint32_t at)
{
 script_stage(c,L,at);
 if(!c->script_temp[slot]){script_fail(c,L,at,GD_LJ204_UNSUPPORTED,"OWN Script temporary ownership missing");return;}
 if(!HeapFree(GetProcessHeap(),0,c->script_temp[slot])){c->script_cleanup_failed=1;script_fail(c,L,at,GD_LJ204_MEMORY,"OWN Script temporary free failed; retained");return;}
 c->script_temp[slot]=NULL;--c->script_result->live_temp_allocations;++c->script_result->normal_temp_releases;
}
static lua_State *script_wrapper_state(gd_context *c,lua_State *L,gd_script_wrapper *wrapper,uint32_t at)
{
 lua_State *actual=wrapper->state;script_stage(c,L,at);++c->script_result->wrapper_reads;
 if(actual!=L){script_fail(c,L,at,GD_LJ204_UNSUPPORTED,"OWN Script wrapper changed VM identity");return NULL;}
 return actual;
}
static void script_core(gd_context *c,lua_State *L,uint32_t kind,int table_index)
{
 uint32_t delta=kind*0x590u,descriptor,flags;int registry;size_t keylen=strlen(script_keys[kind]),full_len;
 gd_lj204_script_binding_result *r=&c->script_result->bindings[kind];gd_script_owned_node *owned=&c->script_nodes[kind];
 unsigned char *key,*copy;void *userdata;gd_gc_own_userdata *entry;gd_script_node_image *node;
 r->phase=3;r->core_table_index=table_index;
 if(script_read(c,L,0xf0092+delta,GD_LJ204_SCRIPT_VERSION)==2&&table_index==LUA_GLOBALSINDEX){
  script_fail(c,L,0xf00ad+delta,GD_LJ204_UNSUPPORTED,"Script alternate-version pseudo-index is outside owned Lua51");return;
 }
 key=script_temp_alloc(c,L,1,keylen+1<8?8:keylen+1,0xf0148+delta);memcpy(key,script_keys[kind],keylen+1);
 script_stage(c,L,0xf016c+delta);c->api.pushstring(L,(const char *)key);
 script_stage(c,L,0xf017c+delta);
 if(table_index<=0||c->api.type(L,table_index)!=LUA_TTABLE){script_fail(c,L,0xf017c+delta,GD_LJ204_UNSUPPORTED,"Script core lacks its owned positive table index");return;}
 c->api.rawget(L,table_index);script_stage(c,L,0xf018e+delta);r->lookup_is_c=c->api.iscfunction(L,-1);
 if(r->lookup_is_c!=0){script_fail(c,L,0xf0199+delta,GD_LJ204_UNSUPPORTED,"Existing C-function overload branch remains unproved");return;}
 script_stage(c,L,0xf03e5+delta);c->api.pushstring(L,(const char *)key);
 script_stage(c,L,0xf03f3+delta);userdata=c->api.newuserdata(L,8);r->userdata_created=1;
 entry=script_track_userdata(c,L,userdata,kind,0xf03f3+delta);
 registry=(int)script_read(c,L,0xf0404+delta,GD_LJ204_SCRIPT_REGISTRY);
 script_stage(c,L,0xf040d+delta);c->api.rawgeti(L,registry,-4);
 script_stage(c,L,0xf041b+delta);c->api.setmetatable(L,-2);
 /* OWN safety observation occurs AFTER the actual setmetatable. A nil or
  * foreign metatable is not repaired: an unowned node must never escape GC. */
 if(!c->api.getmetatable(L,-1)){script_fail(c,L,0xf041b+delta,GD_LJ204_UNSUPPORTED,"Script object GC metatable is absent after actual installation");return;}
 c->api.pushstring(L,own_gc_key);c->api.rawget(L,-2);
 if(c->api.tocfunction(L,-1)!=native_gc_object){script_fail(c,L,0xf041b+delta,GD_LJ204_UNSUPPORTED,"Script object GC closure identity differs");return;}
 c->api.settop(L,-3); /* Only the two OWN observation values. */
 r->phase=4;script_stage(c,L,0xf0426+delta);
 node=(gd_script_node_image *)HeapAlloc(GetProcessHeap(),0,sizeof(*node));
 if(!node){script_fail(c,L,0xf0426+delta,GD_LJ204_MEMORY,"OWN Script node allocation failed");return;}
 owned->node=node;owned->node_live=1;++c->script_audit.nodes_allocated;++c->script_audit.live_nodes;r->node_created=1;
 node->name=NULL;node->capacity=0;node->length=0;node->next=NULL;
 node->vtable=&script_vtables[kind];node->callback=&script_callback_tags[kind];r->known_node_field_mask=63u;
 r->descriptor_known=0;descriptor=(uint32_t)script_read(c,L,0xf0461+delta,GD_LJ204_SCRIPT_DESCRIPTOR);
 node->descriptor=(unsigned char)descriptor;r->descriptor_known=1;r->descriptor_byte=descriptor;r->known_node_field_mask=127u;
 /* Node padding31..37 remains unwritten. The transient name and node's owned
  * copy are separate real allocations, not aliases of helper storage. */
 full_len=strlen((const char *)c->script_temp[0]);
 copy=script_temp_alloc(c,L,2,full_len+1<8?8:full_len+1,0xf0498+delta);
 memcpy(copy,c->script_temp[0],full_len+1);
 script_stage(c,L,0xf04c3+delta); /* The evidenced old name is explicitly NULL. */
 node->length=full_len;node->capacity=full_len+1<8?8:full_len+1;
 script_stage(c,L,0xf04ea+delta);owned->name=(unsigned char *)HeapAlloc(GetProcessHeap(),0,(size_t)node->capacity);
 if(!owned->name){script_fail(c,L,0xf04ea+delta,GD_LJ204_MEMORY,"OWN Script node name allocation failed");return;}
 owned->name_live=1;++c->script_audit.live_node_names;node->name=owned->name;memcpy(node->name,copy,full_len+1);
 script_temp_release(c,L,2,0xf0518+delta);node->next=NULL;
 script_stage(c,L,0xf052a+delta);memcpy(userdata,&node,sizeof(node));
 owned->published=1;entry->initial_payload=node;entry->admitted=1;++c->script_audit.nodes_published;r->node_published=1;
 script_stage(c,L,0xf053d+delta);c->api.pushcclosure(L,script_dispatchers[kind],1);
 script_stage(c,L,0xf054d+delta);c->api.rawset(L,table_index);r->closure_published=1;r->phase=5;
 script_stage(c,L,0xf0562+delta);c->api.settop(L,-2);
 flags=(uint32_t)script_read(c,L,0xf0568+delta,GD_LJ204_SCRIPT_FLAGS);
 if(flags&1u){script_fail(c,L,0xf057c+delta,GD_LJ204_UNSUPPORTED,"Script registration logging vslots/formatter remain unproved");return;}
 script_temp_release(c,L,1,0xf05d2+delta);
}
static void script_helper(gd_context *c,lua_State *L,gd_script_wrapper *wrapper,uint32_t kind)
{
 static const uint32_t alloc_at[3]={0xefb40,0xefd10,0xefef0},grow_at[3]={0xefbda,0xefdaa,0xeff8a},grow_free_at[3]={0xefbf3,0xefdc3,0xeffa3};
 static const uint32_t read_l[3][4]={{0xefc1b,0xefc41,0xefc63,0xefc70},{0xefdee,0xefe14,0xefe36,0xefe43},{0xeffcc,0xefff2,0xf0014,0xf0021}};
 static const uint32_t registry_at[3]={0xefc35,0xefe08,0xeffe6},push_at[3]={0xefc29,0xefdfc,0xeffda};
 static const uint32_t raw_at[3]={0xefc3b,0xefe0e,0xeffec},top_at[3]={0xefc44,0xefe17,0xefff5};
 static const uint32_t core_at[3]={0xefc66,0xefe39,0xf0017},pop_at[3]={0xefc73,0xefe46,0xf0024},free_at[3]={0xefc9b,0xefe6e,0xf004c};
 gd_lj204_script_binding_result *r=&c->script_result->bindings[kind];size_t suffix=strlen(script_keys[kind]),length=7+suffix,capacity=14;
 unsigned char *name,*grown;lua_State *actual;int registry,index;
 c->script_binding_index=(int)kind;r->helper_entered=1;r->phase=1;
 name=script_temp_alloc(c,L,0,capacity,alloc_at[kind]);
 ++r->helper_allocations;memcpy(name,"Script",7);name[6]='.';name[7]=0;
 if(capacity<length+1){
  capacity=2*(length+1);grown=script_temp_alloc(c,L,3,capacity,grow_at[kind]);++r->helper_allocations;
  /* EFDB2/EFF92 copy R15=7 ("Script."), excluding its old NUL. */
  memcpy(grown,name,7);script_temp_release(c,L,0,grow_free_at[kind]);++r->helper_releases;
  c->script_temp[0]=grown;c->script_temp[3]=NULL;name=grown;
 }
 memcpy(name+7,script_keys[kind],suffix+1);r->helper_name_length=(uint32_t)length;r->helper_name_capacity=(uint32_t)capacity;
 r->phase=2;actual=script_wrapper_state(c,L,wrapper,read_l[kind][0]);
 r->wrapper_mode=wrapper->mode;r->wrapper_reference=wrapper->reference;
 if(wrapper->mode==1){script_stage(c,L,push_at[kind]);c->api.pushvalue(actual,wrapper->reference);}
 else{registry=(int)script_read(c,L,registry_at[kind],GD_LJ204_SCRIPT_REGISTRY);script_stage(c,L,raw_at[kind]);c->api.rawgeti(actual,registry,wrapper->reference);}
 actual=script_wrapper_state(c,L,wrapper,read_l[kind][1]);script_stage(c,L,top_at[kind]);index=c->api.gettop(actual);
 actual=script_wrapper_state(c,L,wrapper,read_l[kind][2]);script_stage(c,L,core_at[kind]);script_core(c,actual,kind,index);
 actual=script_wrapper_state(c,L,wrapper,read_l[kind][3]);script_stage(c,L,pop_at[kind]);c->api.settop(actual,-2);
 script_temp_release(c,L,0,free_at[kind]);++r->helper_releases;r->helper_returned=1;r->phase=6;++c->script_result->helpers_completed;
}
static int __cdecl script_initialize_thunk(lua_State *L)
{
 gd_context *c=active_raw_context;gd_lj204_script_result *out=c->script_result;gd_script_wrapper wrapper;
 lua_State *outer=L;int registry,reference;uint32_t i,version;
 c->api.settop(L,0); /* ONLY the OWN cpcall argument frame, never outer prefix. */
 wrapper.state=L;wrapper.mode=0;wrapper.reference=-2;out->phase=1;
 script_stage(c,L,0xef8e4);c->api.createtable(L,0,0);out->table_created=1;
 registry=(int)script_read(c,L,0xef8ea,GD_LJ204_SCRIPT_REGISTRY);
 script_stage(c,L,0xef8f3);reference=c->api.ref(L,registry);out->table_reference=reference;
 wrapper.reference=reference;wrapper.mode=2;out->phase=2;
 for(i=0;i<3;++i)script_helper(c,L,&wrapper,i);
 c->script_binding_index=-1;out->phase=3;version=(uint32_t)script_read(c,L,0xef93d,GD_LJ204_SCRIPT_VERSION);
 if(version>INT32_MAX)return script_fail(c,L,0xef93d,GD_LJ204_UNSUPPORTED,"Script version cannot be represented by this receipt");
 out->outer_version=(int32_t)version;
 if(version==2)return script_fail(c,L,0xef951,GD_LJ204_UNSUPPORTED,"Script alternate global pseudo-index is outside owned Lua51");
 script_stage(c,outer,0xef99e);c->api.pushstring(outer,"Script");
 registry=(int)script_read(c,outer,0xef9a7,GD_LJ204_SCRIPT_REGISTRY);
 script_stage(c,outer,0xef9b0);c->api.rawgeti(outer,registry,reference);
 script_stage(c,outer,0xef9be);c->api.settable(outer,LUA_GLOBALSINDEX);out->global_published=1;
 out->phase=4;registry=(int)script_read(c,outer,0xef9c8,GD_LJ204_SCRIPT_REGISTRY);
 script_stage(c,outer,0xef9d1);c->api.unref(outer,registry,reference);out->reference_released=1;
 out->phase=5;script_stage(c,outer,0xef9e2);out->normal_returned=1;return 0;
}
static gd_script_owned_node *script_checked_node(gd_context *c,lua_State *L,int index,uint32_t kind,uint32_t at)
{
 void *userdata,*payload;gd_gc_own_userdata *entry;gd_script_owned_node *node;
 userdata=c->api.touserdata(L,index);entry=gc_entry(c,userdata);
 if(c->api.type(L,index)!=LUA_TUSERDATA||!entry||!entry->admitted||entry->finalizer_seen||entry->script_index!=kind+1){
  script_fail(c,L,at,GD_LJ204_UNSUPPORTED,"Script closure upvalue is not a live owned userdata");return NULL;
 }
 node=&c->script_nodes[kind];
 if(node->epoch!=entry->script_epoch||node->userdata!=userdata||!node->node_live||!node->published||node->destructor_seen){
  script_fail(c,L,at,GD_LJ204_UNSUPPORTED,"Script closure node epoch/lifetime differs");return NULL;
 }
 memcpy(&payload,userdata,sizeof(payload));
 if(payload!=node->node||node->node->vtable!=&script_vtables[kind]||node->node->callback!=&script_callback_tags[kind]){
  script_fail(c,L,at,GD_LJ204_UNSUPPORTED,"Script closure node identity differs");return NULL;
 }
 return node;
}
static int script_dispatch(lua_State *L,uint32_t kind)
{
 static const uint32_t entry_at[3]={0xf1b40,0xf1bd0,0xf1c80},base_at[3]={0xf1b4a,0xf1bda,0xf1c8a};
 static const uint32_t userdata_at[3]={0xf1b55,0xf1be5,0xf1c95},node_at[3]={0xf1b5e,0xf1bee,0xf1c9e};
 static const uint32_t top_at[3]={0xf1b61,0xf1bf1,0xf1ca1},flags_at[3]={0xf1b7b,0xf1c30,0xf1cce};
 static const uint32_t log_at[3]={0xf1b92,0xf1c47,0xf1ce5},business_at[3]={0xf1b9e,0xf1c53,0xf1cf1},failure_at[3]={0xf1bb6,0xf1c6b,0xf1d09};
 gd_context *c=callback_context(L);gd_script_owned_node *node;int index,count,valid=0;uint32_t flags;
 if(!c)return 0;++c->script_audit.callback_entries;c->script_audit.last_callback_return=-1;
 enter_callback(c,L,entry_at[kind]);index=(int)script_read(c,L,base_at[kind],GD_LJ204_SCRIPT_UPVALUE_BASE)-1;
 script_stage(c,L,userdata_at[kind]);node=script_checked_node(c,L,index,kind,node_at[kind]);
 script_stage(c,L,top_at[kind]);count=c->api.gettop(L);
 if(kind==0&&count==1){script_stage(c,L,0xf1b71);valid=c->api.isstring(L,1)!=0;}
 else if(kind==1&&count==3){
  script_stage(c,L,0xf1c02);valid=c->api.isnumber(L,1)!=0;
  if(valid){script_stage(c,L,0xf1c14);valid=c->api.isstring(L,2)!=0;}
  if(valid){script_stage(c,L,0xf1c26);valid=c->api.isnumber(L,3)!=0;}
 }else if(kind==2&&count==2){
  script_stage(c,L,0xf1cb2);valid=c->api.isnumber(L,1)!=0;
  if(valid){script_stage(c,L,0xf1cc4);valid=c->api.isstring(L,2)!=0;}
 }
 if(!valid)return script_fail(c,L,failure_at[kind],GD_LJ204_UNSUPPORTED,"Script signature-error helper2561D0 remains unproved");
 flags=(uint32_t)script_read(c,L,flags_at[kind],GD_LJ204_SCRIPT_FLAGS);
 if(flags&2u)return script_fail(c,L,log_at[kind],GD_LJ204_UNSUPPORTED,"Script dispatcher logging remains unproved");
 /* node+28 was identity-checked, but the three business helpers are not
  * recovered. Never call the original address or pretend the closure ran. */
 if(node->node->callback!=&script_callback_tags[kind])return script_fail(c,L,business_at[kind],GD_LJ204_UNSUPPORTED,"Script business callback identity changed");
 return script_fail(c,L,business_at[kind],GD_LJ204_UNSUPPORTED,"Script business helper remains unproved");
}
static int __cdecl script_load_dispatch(lua_State *L){return script_dispatch(L,0);}
static int __cdecl script_register_dispatch(lua_State *L){return script_dispatch(L,1);}
static int __cdecl script_unregister_dispatch(lua_State *L){return script_dispatch(L,2);}
static int script_observe_fixture(gd_context *c,lua_State *L)
{
 uint32_t i;stage(c,L,0);c->api.pushstring(L,"Script");c->api.rawget(L,LUA_GLOBALSINDEX);
 if(c->api.type(L,-1)!=LUA_TTABLE)return script_fail(c,L,0,GD_LJ204_UNSUPPORTED,"OWN Script observation: global table absent");
 for(i=0;i<3;++i){
  c->api.pushstring(L,script_keys[i]);c->api.rawget(L,1);
  if(c->api.tocfunction(L,-1)!=script_dispatchers[i])return script_fail(c,L,0,GD_LJ204_UNSUPPORTED,"OWN Script observation: C closure identity differs");
  if(!c->api.getupvalue(L,-1,1))return script_fail(c,L,0,GD_LJ204_UNSUPPORTED,"OWN Script observation: closure upvalue absent");
  if(!script_checked_node(c,L,-1,i,0))return 0;
  c->api.settop(L,1);
 }
 c->api.settop(L,0);c->result->flags|=GD_FLAG_SCRIPT_OBSERVED;return 0;
}
static void script_own_cleanup(gd_context *c)
{
 gd_lj204_script_result *out=c->script_result;gd_script_owned_node *node;uint32_t i;
 if(c->script_cleanup_failed)return; /* Do not retry a failed native release. */
 for(i=0;i<4;++i)if(c->script_temp[i]){
  if(!HeapFree(GetProcessHeap(),0,c->script_temp[i])){c->script_cleanup_failed=1;out->call.bridge_status=GD_LJ204_MEMORY;return;}
  c->script_temp[i]=NULL;--out->live_temp_allocations;++out->own_temp_releases;
 }
 for(i=0;i<3;++i){node=&c->script_nodes[i];if(node->published)continue;
  if(node->name_live){
   if(!HeapFree(GetProcessHeap(),0,node->name)){c->script_cleanup_failed=1;out->call.bridge_status=GD_LJ204_MEMORY;return;}
   node->name_live=0;--c->script_audit.live_node_names;++c->script_audit.own_unpublished_name_frees;
  }
  if(node->node_live){
   if(!HeapFree(GetProcessHeap(),0,node->node)){c->script_cleanup_failed=1;out->call.bridge_status=GD_LJ204_MEMORY;return;}
   node->node_live=0;--c->script_audit.live_nodes;++c->script_audit.own_unpublished_node_frees;
  }
 }
}
static void init_script_result(gd_lj204_script_result *out,uint64_t token)
{
 uint32_t i;memset(out,0,sizeof(*out));init_result(&out->call,token,0);out->struct_bytes=(uint32_t)sizeof(*out);
 out->table_reference=-1;out->outer_version=-1;out->prefix_identity_preserved=-1;
 for(i=0;i<3;++i){gd_lj204_script_binding_result *r=&out->bindings[i];r->kind=i;r->wrapper_mode=-1;r->wrapper_reference=-1;r->core_table_index=-1;r->lookup_is_c=-1;r->descriptor_known=-1;}
}
int32_t __cdecl gd_lj204_initialize_script(uint64_t token,gd_lj204_script_reader reader,void *cookie,gd_lj204_script_result *out)
{
 gd_context *c,*prior;int32_t admission=GD_LJ204_OK;int status;const char *prefix=NULL,*after;size_t prefix_bytes=0,after_bytes=0;
 if(!out)return GD_LJ204_INVALID;init_script_result(out,token);
 if(!reader){out->call.bridge_status=GD_LJ204_INVALID;return GD_LJ204_INVALID;}
 AcquireSRWLockExclusive(&contexts_lock);c=find_token(token);
 if(!c||c->closing)admission=GD_LJ204_CLOSED;
 else if(c->active||active_raw_context)admission=GD_LJ204_BUSY;
 else if(c->owner_thread!=GetCurrentThreadId())admission=GD_LJ204_WRONG_THREAD;
 else if(c->published!=127u||c->script_attempted)admission=GD_LJ204_UNSUPPORTED;
 else{c->active=1;c->result=&out->call;c->script_result=out;c->script_reader=reader;c->script_cookie=cookie;c->script_attempted=1;}
 ReleaseSRWLockExclusive(&contexts_lock);
 if(admission!=GD_LJ204_OK){out->call.bridge_status=(uint32_t)admission;return admission;}
 prior=active_raw_context;active_raw_context=c;out->call.entry_top=c->api.gettop(c->state);
 /* For an already-string TValue, lua_tolstring returns its existing bytes;
  * no number conversion/allocation is attempted outside protected execution. */
 if(out->call.entry_top==1&&c->api.type(c->state,1)==LUA_TSTRING)prefix=c->api.tolstring(c->state,1,&prefix_bytes);
 status=c->api.cpcall(c->state,script_initialize_thunk,NULL);out->call.lua_status=status;
 if(status!=0){out->call.flags|=GD_FLAG_UNWIND;if(out->call.bridge_status==GD_LJ204_OK)out->call.bridge_status=GD_LJ204_LUA_ERROR;}
 out->call.exit_top=c->api.gettop(c->state);
 if(status!=0&&out->call.exit_top>out->call.entry_top)out->call.flags|=GD_FLAG_ERROR_OBJECT;
 if(out->call.entry_top==0)out->prefix_identity_preserved=1;
 else if(prefix){out->prefix_identity_preserved=0;
  if(out->call.exit_top>=1&&c->api.type(c->state,1)==LUA_TSTRING){after=c->api.tolstring(c->state,1,&after_bytes);out->prefix_identity_preserved=after==prefix&&after_bytes==prefix_bytes?1:0;}
 }
 script_own_cleanup(c);clear_scratch(c);c->callback_depth=0;c->gc_depth=0;active_raw_context=prior;
 out->call.published_mask=c->published;
 AcquireSRWLockExclusive(&contexts_lock);c->result=NULL;c->script_result=NULL;c->script_binding_index=-1;c->active=0;ReleaseSRWLockExclusive(&contexts_lock);
 return (int32_t)out->call.bridge_status;
}
int32_t __cdecl gd_lj204_read_script_audit(uint64_t token,gd_lj204_script_audit *out)
{
 gd_context *c;int32_t result=GD_LJ204_OK;if(!out)return GD_LJ204_INVALID;
 memset(out,0,sizeof(*out));out->struct_bytes=(uint32_t)sizeof(*out);out->context_token=token;out->last_callback_return=-1;
 AcquireSRWLockShared(&contexts_lock);c=find_token(token);
 if(!c)result=GD_LJ204_CLOSED;else if(c->active||c->gc_depth||c->callback_depth)result=GD_LJ204_BUSY;
 else if(GetCurrentThreadId()!=(c->closing?c->close_thread:c->owner_thread))result=GD_LJ204_WRONG_THREAD;
 else *out=c->script_audit;ReleaseSRWLockShared(&contexts_lock);return result;
}
/* Each row describes ONE entered true branch. The original bool tests and
 * separate service+00 captures belong to the managed service contract. */
typedef struct gd_role_sites {
 uint32_t push_boolean,ref_read,ref_call,version_read,alternate_call;
 uint32_t push_name,lookup_read,lookup_call,set_global,unref_read,unref_call,complete;
} gd_role_sites;
static const gd_role_sites role_sites[2]={
 {0xf2f4e,0xf2f54,0xf2f5d,0xf2f74,0xf2f88,0xf2fd5,0xf2fde,0xf2fe7,0xf2ff5,0xf2fff,0xf3008,0xf300f},
 {0xf3038,0xf303e,0xf3047,0xf305e,0xf3072,0xf30bf,0xf30c8,0xf30d1,0xf30df,0xf30e9,0xf30f2,0xf30f9}
};
static const char *const role_names[2]={"Client","Server"};
static int64_t role_read(gd_context *c,lua_State *L,uint32_t at,uint32_t requested)
{
 int64_t value=0;int32_t answer;
 stage(c,L,at);++c->result->resolver_calls;
 answer=c->role_reader(c->role_cookie,at,requested,&value);
 /* The synchronous managed POD frame has returned before lua_error. */
 if(answer!=0){c->result->host_status=answer;
  fail(c,L,answer==2?GD_LJ204_HOST_ERROR:GD_LJ204_UNSUPPORTED,"OWN role live field unknown");return 0;}
 if((requested==GD_LJ204_SCRIPT_REGISTRY&&value!=LUA_REGISTRYINDEX)||
    (requested==GD_LJ204_SCRIPT_VERSION&&(value<0||value>UINT32_MAX))){
  fail(c,L,GD_LJ204_UNSUPPORTED,"OWN role field outside its typed Lua51 profile");return 0;
 }
 return value;
}
static int __cdecl role_publish_thunk(lua_State *L)
{
 gd_context *c=active_raw_context;gd_lj204_role_result *out=c->role_result;
 const gd_role_sites *sites=&role_sites[out->kind];int registry;
 c->api.settop(L,0); /* OWN protected argument frame only; outer prefix survives. */
 out->phase=1;stage(c,L,sites->push_boolean);c->api.pushboolean(L,1);out->boolean_pushed=1;
 out->phase=2;registry=(int)role_read(c,L,sites->ref_read,GD_LJ204_SCRIPT_REGISTRY);
 stage(c,L,sites->ref_call);out->reference=c->api.ref(L,registry);
 out->phase=3;out->version_known=0;
 out->version_dword=(uint32_t)role_read(c,L,sites->version_read,GD_LJ204_SCRIPT_VERSION);out->version_known=1;
 if(out->version_dword==2){stage(c,L,sites->alternate_call);
  return fail(c,L,GD_LJ204_UNSUPPORTED,"Role alternate global pseudo-index is outside owned Lua51");}
 out->phase=4;stage(c,L,sites->push_name);c->api.pushstring(L,role_names[out->kind]);
 registry=(int)role_read(c,L,sites->lookup_read,GD_LJ204_SCRIPT_REGISTRY);
 stage(c,L,sites->lookup_call);c->api.rawgeti(L,registry,out->reference);
 stage(c,L,sites->set_global);c->api.settable(L,LUA_GLOBALSINDEX);out->global_published=1;
 out->phase=5;registry=(int)role_read(c,L,sites->unref_read,GD_LJ204_SCRIPT_REGISTRY);
 stage(c,L,sites->unref_call);c->api.unref(L,registry,out->reference);out->reference_released=1;
 out->phase=6;stage(c,L,sites->complete);out->normal_returned=1;return 0;
}
static int role_observe_fixture(gd_context *c,lua_State *L)
{
 uint32_t i;int type;static const uint32_t boolean_bits[2]={GD_FLAG_CLIENT_BOOLEAN,GD_FLAG_SERVER_BOOLEAN};
 static const uint32_t true_bits[2]={GD_FLAG_CLIENT_TRUE,GD_FLAG_SERVER_TRUE};
 stage(c,L,0); /* Explicit OWN observation, no invented original call site. */
 for(i=0;i<2;++i){
  c->api.pushstring(L,role_names[i]);c->api.rawget(L,LUA_GLOBALSINDEX);type=c->api.type(L,-1);
  if(type!=LUA_TNIL&&type!=LUA_TBOOLEAN)return fail(c,L,GD_LJ204_UNSUPPORTED,"OWN role observation requires nil or boolean");
  if(type==LUA_TBOOLEAN){c->result->flags|=boolean_bits[i];if(c->api.toboolean(L,-1))c->result->flags|=true_bits[i];}
  c->api.settop(L,-2);
 }
 c->result->flags|=GD_FLAG_ROLES_OBSERVED;return 0;
}
static void init_role_result(gd_lj204_role_result *out,uint64_t token,uint32_t kind)
{
 memset(out,0,sizeof(*out));init_result(&out->call,token,0);out->struct_bytes=(uint32_t)sizeof(*out);out->kind=kind;
 out->reference=-1;out->version_known=-1;out->prefix_identity_preserved=-1;
}
int32_t __cdecl gd_lj204_publish_service_role(uint64_t token,uint32_t kind,
 gd_lj204_script_reader reader,void *cookie,gd_lj204_role_result *out)
{
 gd_context *c,*prior;int32_t admission=GD_LJ204_OK;int status;const char *prefix=NULL,*after;size_t prefix_bytes=0,after_bytes=0;
 if(!out)return GD_LJ204_INVALID;init_role_result(out,token,kind);
 if(!reader||kind>GD_LJ204_ROLE_SERVER){out->call.bridge_status=GD_LJ204_INVALID;return GD_LJ204_INVALID;}
 AcquireSRWLockExclusive(&contexts_lock);c=find_token(token);
 if(!c||c->closing)admission=GD_LJ204_CLOSED;
 else if(c->active||active_raw_context)admission=GD_LJ204_BUSY;
 else if(c->owner_thread!=GetCurrentThreadId())admission=GD_LJ204_WRONG_THREAD;
 else if(c->published!=127u||(c->role_attempted_mask&(1u<<kind)))admission=GD_LJ204_UNSUPPORTED;
 else{c->active=1;c->result=&out->call;c->role_result=out;c->role_reader=reader;c->role_cookie=cookie;c->role_attempted_mask|=1u<<kind;}
 ReleaseSRWLockExclusive(&contexts_lock);
 if(admission!=GD_LJ204_OK){out->call.bridge_status=(uint32_t)admission;return admission;}
 prior=active_raw_context;active_raw_context=c;out->call.entry_top=c->api.gettop(c->state);
 if(out->call.entry_top==1&&c->api.type(c->state,1)==LUA_TSTRING)prefix=c->api.tolstring(c->state,1,&prefix_bytes);
 status=c->api.cpcall(c->state,role_publish_thunk,NULL);out->call.lua_status=status;
 if(status!=0){out->call.flags|=GD_FLAG_UNWIND;if(out->call.bridge_status==GD_LJ204_OK)out->call.bridge_status=GD_LJ204_LUA_ERROR;}
 out->call.exit_top=c->api.gettop(c->state);
 if(status!=0&&out->call.exit_top>out->call.entry_top)out->call.flags|=GD_FLAG_ERROR_OBJECT;
 if(out->call.entry_top==0)out->prefix_identity_preserved=1;
 else if(prefix){out->prefix_identity_preserved=0;
  if(out->call.exit_top>=1&&c->api.type(c->state,1)==LUA_TSTRING){after=c->api.tolstring(c->state,1,&after_bytes);out->prefix_identity_preserved=after==prefix&&after_bytes==prefix_bytes?1:0;}
 }
 /* No unref rollback: the reference and global failure prefix are real. */
 clear_scratch(c);c->callback_depth=0;c->gc_depth=0;active_raw_context=prior;out->call.published_mask=c->published;
 AcquireSRWLockExclusive(&contexts_lock);c->result=NULL;c->role_result=NULL;c->role_reader=NULL;c->role_cookie=NULL;c->active=0;ReleaseSRWLockExclusive(&contexts_lock);
 return (int32_t)out->call.bridge_status;
}
int32_t __cdecl gd_lj204_begin_close(uint64_t token)
{
 gd_context *c;int32_t result=GD_LJ204_OK;
 AcquireSRWLockExclusive(&contexts_lock);c=find_token(token);
 if(!c)result=GD_LJ204_CLOSED;
 else if(c->active||c->closing||active_raw_context)result=GD_LJ204_BUSY;
 else{
  c->closing=1;c->close_thread=GetCurrentThreadId();init_result(&c->close_result,token,0);
  c->close_result.published_mask=c->published;c->result=&c->close_result;
 }
 ReleaseSRWLockExclusive(&contexts_lock);return result;
}
int32_t __cdecl gd_lj204_complete_close(uint64_t token,gd_lj204_result *close_result)
{
 gd_context *c,**link;int32_t result=GD_LJ204_OK;
 if(!close_result)return GD_LJ204_INVALID;init_result(close_result,token,0);
 AcquireSRWLockExclusive(&contexts_lock);c=find_token(token);
 if(!c)result=GD_LJ204_CLOSED;
 else if(!c->closing||c->active)result=GD_LJ204_BUSY;
 else if(c->close_thread!=GetCurrentThreadId())result=GD_LJ204_WRONG_THREAD;
 else if(c->gc_audit.live_own_buffers!=0||c->gl_full_storage||c->script_audit.live_nodes||c->script_audit.live_node_names||c->script_cleanup_failed)result=GD_LJ204_UNSUPPORTED;
 else{
  /* The owning runtime calls here only after its single lua_close returned.
   * Release failure keeps context and all pointers pinned for diagnosis. */
  if(!FreeLibrary(c->module))result=GD_LJ204_MODULE;
  else{
   *close_result=c->close_result;close_result->flags|=GD_FLAG_CLOSE_RETURNED;
   for(link=&contexts;*link!=c;link=&(*link)->next){}
   *link=c->next;clear_scratch(c);HeapFree(GetProcessHeap(),0,c);
  }
 }
 ReleaseSRWLockExclusive(&contexts_lock);
 if(result!=GD_LJ204_OK)close_result->bridge_status=(uint32_t)result;
 return result;
}
