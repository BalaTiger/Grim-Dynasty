import struct as s,unittest
from quest_structure import quest_structure,Cursor
from gd_resources import FormatError
def u(n):return s.pack('<I',n)
def text(v):b=v.encode();return u(len(b))+b
def fixture(condition=18,version=0,boolean=1,operator=0,extra=b'',strings=5):
 # One task, one objective, no events; explicit independent wire fixture.
 cond=u(condition)+bytes([version])+u(2)+text('BRIDGE_READY')+bytes([boolean])
 objective=u(30)+u(0)+u(1)+u(operator)+cond+u(1)+u(4)+b'\0'+u(1000)
 graph=u(10)+u(1)+u(1)+u(20)+u(0)+u(0)+u(1)+objective+u(0)+b'\1\0'+extra
 locales=u(1)+text('enUS')+u(strings)+b''.join(text(x) for x in ['Group','Quest','Task','Journal','Repair bridge'][:strings])
 return b'QST2'+u(9)+u(12+len(graph))+graph+locales

class QuestTests(unittest.TestCase):
 def test_typed_graph_and_text_binding(self):
  q=quest_structure(fixture());t=q['tasks'][0];o=t['objectives'][0]
  self.assertEqual(q['uid'],10);self.assertTrue(t['is_blocker']);self.assertEqual(o['uid'],30)
  self.assertEqual(o['conditions']['conditions'][0]['parameters'],{'token':'BRIDGE_READY','has':True})
  self.assertEqual(o['actions'][0]['parameters'],{'amount':1000})
  self.assertEqual(o['text']['enUS']['index'],4);self.assertEqual(o['text']['enUS']['text'],'Repair bridge')
 def test_unknown_type_not_skipped(self):
  with self.assertRaisesRegex(FormatError,'Unsupported condition type 999'):quest_structure(fixture(condition=999))
 def test_unknown_version_not_guessed(self):
  with self.assertRaisesRegex(FormatError,'Unverified script version'):quest_structure(fixture(version=9))
 def test_noncanonical_boolean(self):
  with self.assertRaisesRegex(FormatError,'Noncanonical bool'):quest_structure(fixture(boolean=2))
 def test_unknown_operator(self):
  with self.assertRaisesRegex(FormatError,'condition operator'):quest_structure(fixture(operator=2))
 def test_graph_extra_byte(self):
  with self.assertRaisesRegex(FormatError,'not exactly consumed'):quest_structure(fixture(extra=b'x'))
 def test_localization_count_mismatch(self):
  with self.assertRaisesRegex(FormatError,'text-node count mismatch'):quest_structure(fixture(strings=4))
 def test_graph_cannot_read_locale_bytes(self):
  b=bytearray(fixture());s.pack_into('<I',b,20,10001)
  with self.assertRaisesRegex(FormatError,'collection count limit'):quest_structure(b)
 def test_lua_typed_payload_does_not_execute(self):
  body=u(11)+b'\1'+text('chapter.callback')+u(1)+u(4)+b'\0'+s.pack('<f',1.5)+text('quest-value')
  c=Cursor(b'\0'*12+body,12+len(body));r=c.script(False)
  self.assertEqual(r['parameters']['function'],'chapter.callback')
  self.assertEqual(r['parameters']['arguments'][0]['string_value'],'quest-value')
  self.assertEqual(c.p,c.end)
 def test_lua_invalid_type_rejected(self):
  body=u(11)+b'\1'+text('callback')+u(1)+u(99)
  c=Cursor(b'\0'*12+body,12+len(body))
  with self.assertRaisesRegex(FormatError,'Lua argument type'):c.script(False)

if __name__=='__main__':unittest.main(verbosity=2)
